import { expect, test } from '@playwright/test'

test('guest books an event type slot and owner sees the booking', async ({ page }) => {
  const uniqueSuffix = Date.now().toString()
  const guestName = `Гость E2E ${uniqueSuffix}`
  const guestEmail = `e2e-booking-${uniqueSuffix}@example.com`

  await page.goto('/book')

  await expect(page.getByRole('heading', { name: 'Записаться' })).toBeVisible()
  await expect(page.getByText('Алексей Петров')).toBeVisible()

  await page.getByRole('link', { name: /Вводная встреча/ }).click()

  await expect(page.getByRole('heading', { name: 'Свободное время' })).toBeVisible()
  const firstSlot = page.getByRole('button', { name: /\d{2}:\d{2}-\d{2}:\d{2}/ }).first()
  await expect(firstSlot).toBeVisible()
  await firstSlot.click()

  await page.getByRole('button', { name: 'Продолжить' }).click()

  await expect(page.getByRole('heading', { name: 'Ваши данные' })).toBeVisible()
  await page.getByLabel('Имя').fill(guestName)
  await page.getByLabel('Email').fill(guestEmail)
  await page.getByRole('button', { name: 'Записаться' }).click()

  await expect(page.getByRole('heading', { name: 'Запись создана' })).toBeVisible()
  await expect(page.getByText(guestName)).toBeVisible()
  await expect(page.getByText(guestEmail)).toBeVisible()
  await expect(page.getByText('Вводная встреча')).toBeVisible()

  await page.goto('/admin/upcoming')

  await expect(page.getByRole('heading', { name: 'Предстоящие встречи' })).toBeVisible()
  await expect(page.getByText(guestName)).toBeVisible()
  await expect(page.getByText(guestEmail)).toBeVisible()
  await expect(page.getByText('Вводная встреча')).toBeVisible()
})
