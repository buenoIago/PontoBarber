import AxeBuilder from '@axe-core/playwright';
import { expect, test } from '@playwright/test';

test('escolha de perfil exibe as opções de cliente e profissional', async ({ page }) => {
  await page.goto('/');

  await expect(page.getByRole('button', { name: 'Sou cliente' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Sou profissional' })).toBeVisible();
});

test('fluxo de cadastro de cliente envia o formulário', async ({ page }) => {
  await page.goto('/');
  await page.getByRole('button', { name: 'Sou cliente' }).click();

  await expect(page.getByRole('heading', { name: 'Criar conta' })).toBeVisible();

  await page.locator('input#nome').fill('Cliente E2E');
  await page.locator('input#email').fill('cliente-e2e@exemplo.com');
  await page.locator('input#telefone').fill('11999999999');
  await page.locator('input#senha').fill('Senha123');
  await page.locator('input#confirmarSenha').fill('Senha123');

  await page.getByRole('button', { name: 'Cadastrar' }).click();
  await expect(page.locator('.alert')).toBeVisible();
});

test('fluxo de login compartilhado envia credenciais', async ({ page }) => {
  await page.goto('/');
  await page.getByRole('button', { name: 'Entrar' }).click();

  await expect(page.getByRole('heading', { name: 'Entrar' })).toBeVisible();

  await page.locator('input#login-email').fill('usuario@exemplo.com');
  await page.locator('input#login-senha').fill('Senha123');
  await page.getByRole('button', { name: 'Entrar', exact: true }).last().click();

  await expect(page.locator('.alert')).toBeVisible();
});

test('página inicial não apresenta violações críticas de acessibilidade', async ({ page }) => {
  await page.goto('/');

  const resultados = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
    .analyze();

  const criticas = resultados.violations.filter((violacao) => violacao.impact === 'critical');
  expect(criticas, criticas.map((violacao) => violacao.help).join('; ')).toEqual([]);
});
