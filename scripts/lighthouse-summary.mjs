import { appendFile, readdir, readFile, writeFile } from 'node:fs/promises'
import path from 'node:path'

const reportDir = process.argv[2] ?? 'lighthouse-report'
const outputFile = process.argv[3] ?? 'lighthouse-findings.md'
const maxFindings = 10

const categoryIds = ['performance', 'accessibility', 'best-practices', 'seo']
const ignoredScoreModes = new Set(['notApplicable', 'manual', 'informative'])

const collectJsonFiles = async (directory) => {
  const entries = await readdir(directory, { withFileTypes: true })
  const files = []

  for (const entry of entries) {
    const entryPath = path.join(directory, entry.name)
    if (entry.isDirectory()) {
      files.push(...await collectJsonFiles(entryPath))
      continue
    }

    if (entry.isFile() && entry.name.endsWith('.json')) {
      files.push(entryPath)
    }
  }

  return files
}

const getPagePath = (report) => {
  const url = report.finalDisplayedUrl ?? report.finalUrl ?? report.requestedUrl
  if (!url) {
    return 'unknown'
  }

  try {
    const parsedUrl = new URL(url)
    return `${parsedUrl.pathname}${parsedUrl.search}` || '/'
  } catch {
    return url
  }
}

const formatScore = (score) => {
  if (typeof score !== 'number') {
    return 'n/a'
  }

  return String(Math.round(score * 100))
}

const average = (items) => items.reduce((sum, item) => sum + item, 0) / items.length

const escapeCell = (value) => String(value).replaceAll('|', '\\|').replaceAll('\n', ' ')

const getMetric = (audit) => {
  if (audit.displayValue) {
    return audit.displayValue
  }

  if (audit.details?.overallSavingsMs) {
    return `${Math.round(audit.details.overallSavingsMs)} ms`
  }

  if (audit.details?.overallSavingsBytes) {
    return `${Math.round(audit.details.overallSavingsBytes / 1024)} KiB`
  }

  return '-'
}

const readReports = async () => {
  let files = []

  try {
    files = await collectJsonFiles(reportDir)
  } catch {
    return []
  }

  const reports = []

  for (const file of files) {
    try {
      const report = JSON.parse(await readFile(file, 'utf8'))
      if (report.categories && report.audits) {
        reports.push(report)
      }
    } catch {
      // Ignore non-Lighthouse JSON files that may be written by LHCI.
    }
  }

  return reports
}

const buildSummary = (reports) => {
  const lines = [
    '# Lighthouse findings',
    '',
    `Generated at: ${new Date().toISOString()}`,
    '',
  ]

  if (reports.length === 0) {
    lines.push('Lighthouse JSON reports were not found. Check the workflow logs for collection errors.', '')
    return lines.join('\n')
  }

  const byPage = new Map()
  const findings = new Map()

  for (const report of reports) {
    const pagePath = getPagePath(report)
    const pageReports = byPage.get(pagePath) ?? []
    pageReports.push(report)
    byPage.set(pagePath, pageReports)

    for (const [auditId, audit] of Object.entries(report.audits)) {
      if (audit.score === null || audit.score === 1 || ignoredScoreModes.has(audit.scoreDisplayMode)) {
        continue
      }

      const current = findings.get(auditId) ?? {
        id: auditId,
        title: audit.title,
        score: 1,
        pages: new Set(),
        metric: getMetric(audit),
      }

      current.score = Math.min(current.score, audit.score ?? 0)
      current.pages.add(pagePath)
      if (current.metric === '-' && getMetric(audit) !== '-') {
        current.metric = getMetric(audit)
      }
      findings.set(auditId, current)
    }
  }

  lines.push('## Scores', '')
  lines.push('| Page | Performance | Accessibility | Best Practices | SEO |')
  lines.push('| --- | ---: | ---: | ---: | ---: |')

  for (const [pagePath, pageReports] of [...byPage.entries()].sort(([left], [right]) => left.localeCompare(right))) {
    const scores = categoryIds.map((categoryId) => {
      const categoryScores = pageReports
        .map((report) => report.categories[categoryId]?.score)
        .filter((score) => typeof score === 'number')

      return categoryScores.length > 0 ? formatScore(average(categoryScores)) : 'n/a'
    })

    lines.push(`| ${escapeCell(pagePath)} | ${scores.join(' | ')} |`)
  }

  lines.push('', '## Candidate fixes', '')

  const topFindings = [...findings.values()]
    .sort((left, right) => left.score - right.score || right.pages.size - left.pages.size || left.title.localeCompare(right.title))
    .slice(0, maxFindings)

  if (topFindings.length === 0) {
    lines.push('No clear Lighthouse-driven fixes were detected. Review the full HTML reports before deciding that no changes are needed.', '')
    return lines.join('\n')
  }

  lines.push('| Audit | Lowest score | Pages | Metric |')
  lines.push('| --- | ---: | --- | --- |')

  for (const finding of topFindings) {
    lines.push(`| ${escapeCell(finding.title)} | ${formatScore(finding.score)} | ${escapeCell([...finding.pages].sort().join(', '))} | ${escapeCell(finding.metric)} |`)
  }

  lines.push('', 'Review these items against the full HTML reports before creating implementation tasks.', '')
  return lines.join('\n')
}

const summary = buildSummary(await readReports())
await writeFile(outputFile, summary)

if (process.env.GITHUB_STEP_SUMMARY) {
  await appendFile(process.env.GITHUB_STEP_SUMMARY, `${summary}\n`)
}
