import AxeBuilder from '@axe-core/playwright';
import { expect, test } from '@playwright/test';

test('escolha de perfil exibe as opções de cliente e profissional', async ({ page }) => {
  await page.goto('/');

  await expect(page.getByRole('button', { name: 'Sou cliente' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Sou profissional' })).toBeVisible();
});

test('cadastro de cliente não solicita senha', async ({ page }) => {
  await page.goto('/');
  await page.getByRole('button', { name: 'Sou cliente' }).click();

  await expect(page.getByRole('heading', { name: 'Criar conta' })).toBeVisible();
  await expect(page.locator('input#senha')).toHaveCount(0);
  await expect(page.locator('input#confirmarSenha')).toHaveCount(0);

  await page.locator('input#nome').fill('Cliente E2E');
  await page.locator('input#email').fill('cliente-e2e@exemplo.com');
  await page.locator('input#telefone').fill('11999999999');

  await page.getByRole('button', { name: 'Cadastrar' }).click();
  await expect(page.locator('.alert')).toBeVisible();
});

test('login de cliente solicita código por e-mail', async ({ page }) => {
  await page.goto('/');
  await page.getByRole('button', { name: 'Entrar como cliente' }).click();

  await expect(page.getByRole('heading', { name: 'Entrar' })).toBeVisible();
  await expect(page.locator('input#login-senha')).toHaveCount(0);

  await page.locator('input#login-email').fill('usuario@exemplo.com');
  await page.getByRole('button', { name: 'Enviar código' }).click();

  await expect(page.locator('.alert')).toBeVisible();
});

test('login profissional mantém e-mail e senha', async ({ page }) => {
  await page.goto('/');
  await page.getByRole('button', { name: 'Entrar como profissional' }).click();

  await expect(page.locator('input#login-senha')).toBeVisible();
  await page.locator('input#login-email').fill('usuario@exemplo.com');
  await page.locator('input#login-senha').fill('Senha123');
  await page.getByRole('button', { name: 'Entrar', exact: true }).last().click();

  await expect(page.locator('.alert')).toBeVisible();
});

test('múltiplos vínculos ativos exigem seleção explícita da barbearia', async ({ page }) => {
  await page.route('**/api/v1/auth/me', (rota) =>
    rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        id: 'u1',
        nomeCompleto: 'Profissional',
        email: 'prof@exemplo.com',
        telefone: null,
        tipo: 'Professional',
        vinculos: [
          {
            id: 'v1',
            barbeariaId: 'b1',
            profissionalId: 'p1',
            cargo: 'Proprietario',
            situacao: 'Ativo',
          },
          {
            id: 'v2',
            barbeariaId: 'b2',
            profissionalId: 'p2',
            cargo: 'Barbeiro',
            situacao: 'Ativo',
          },
        ],
      }),
    }),
  );

  await page.goto('/');
  await expect(page.getByText('Você possui mais de uma barbearia ativa.')).toBeVisible();

  await page.getByRole('button', { name: 'Barbearia b2' }).click();

  await expect(page.getByText('Você possui mais de uma barbearia ativa.')).toBeHidden();
});

test('um único vínculo ativo é restaurado após recarregar a página', async ({ page }) => {
  await page.route('**/api/v1/auth/me', (rota) =>
    rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        id: 'u1',
        nomeCompleto: 'Profissional',
        email: 'prof@exemplo.com',
        telefone: null,
        tipo: 'Professional',
        vinculos: [
          {
            id: 'v1',
            barbeariaId: 'b1',
            profissionalId: 'p1',
            cargo: 'Barbeiro',
            situacao: 'Ativo',
          },
        ],
      }),
    }),
  );

  await page.goto('/');
  await expect(page.getByText('Você possui mais de uma barbearia ativa.')).toHaveCount(0);
  await expect(page.getByRole('navigation', { name: 'Navegação principal' })).toBeVisible();

  await page.reload();

  await expect(page.getByText('Você possui mais de uma barbearia ativa.')).toHaveCount(0);
  await expect(page.getByRole('navigation', { name: 'Navegação principal' })).toBeVisible();
});

test('página inicial não apresenta violações críticas de acessibilidade', async ({ page }) => {
  await page.goto('/');

  const resultados = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
    .analyze();

  const criticas = resultados.violations.filter((violacao) => violacao.impact === 'critical');
  expect(criticas, criticas.map((violacao) => violacao.help).join('; ')).toEqual([]);
});
