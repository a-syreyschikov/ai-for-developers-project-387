module.exports = {
  ci: {
    collect: {
      url: [
        'http://127.0.0.1:8080/',
        'http://127.0.0.1:8080/book',
        'http://127.0.0.1:8080/admin/upcoming',
      ],
      numberOfRuns: 3,
      settings: {
        chromeFlags: '--no-sandbox --disable-dev-shm-usage',
      },
    },
    upload: {
      target: 'filesystem',
      outputDir: '../../lighthouse-report',
    },
  },
}
