import { test, expect } from '@playwright/test'

const TEST_EMAIL = 'user@beautybook.com'
const TEST_PASSWORD = 'Test1234!'

test.describe('Authentication', () => {
  test('login happy path redirects away from login page', async ({ page }) => {
    await page.goto('/login')
    await page.fill('input[type="email"]', TEST_EMAIL)
    await page.fill('input[type="password"]', TEST_PASSWORD)
    await page.click('button[type="submit"]')
    await expect(page).not.toHaveURL(/\/login/)
    await expect(page.getByRole('button', { name: /logout/i })).toBeVisible()
  })

  test('logout clears session and shows login link', async ({ page }) => {
    await page.goto('/login')
    await page.fill('input[type="email"]', TEST_EMAIL)
    await page.fill('input[type="password"]', TEST_PASSWORD)
    await page.click('button[type="submit"]')
    await expect(page).not.toHaveURL(/\/login/)

    await page.getByRole('button', { name: /logout/i }).click()
    await expect(page.getByRole('link', { name: /login/i })).toBeVisible()
  })
})
