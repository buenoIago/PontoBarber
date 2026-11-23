import AxeBuilder from '@axe-core/playwright';
import { expect, test, type Page } from '@playwright/test';

const contaDono = {
  id: '11111111-1111-1111-1111-111111111111',
  nomeCompleto: 'Dono E2E',
  email: 'dono-e2e@exemplo.com',
  telefone: '11999999999',
  tipo: 'Professional',
};

const resumo = {
  hoje: 3,
  estaSemana: 12,
  cancelamentos: 2,
  recebidoNoMes: 1250.5,
  proximos: [
    {
      id: 'a1',
      iniciaEmUtc: '2026-11-23T13:00:00Z',
      nomeCliente: 'Cliente Um',
      nomeProfissional: 'Barbeiro Um',
      situacao: 'Confirmado',
    },
  ],
};

async function preparar(page: Page, dados = resumo): Promise<void> {
  await page.route('**/api/v1/auth/me', (rota) =>
    rota.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(contaDono) }),
  );
  await page.route('**/api/v1/barbearias', (rota) => {
    if (rota.request().method() === 'POST') {
      return rota.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'b1', nome: 'Barbearia E2E', telefoneComercial: '(11) 99999-9999',
          emailComercial: 'contato@exemplo.com', cidade: 'São Paulo', endereco: 'Rua Central, 100',
          instagram: null, codigoConvite: 'ABC23456', fusoHorarioId: 'America/Sao_Paulo',
        }),
      });
    }
    return rota.continue();
  });
  await page.route('**/api/v1/barbearias/b1', (rota) =>
    rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        id: 'b1', nome: 'Barbearia E2E', telefoneComercial: '(11) 99999-9999',
        emailComercial: 'contato@exemplo.com', cidade: 'São Paulo', endereco: 'Rua Central, 100',
        instagram: null, codigoConvite: 'ABC23456', fusoHorarioId: 'America/Sao_Paulo',
      }),
    }),
  );
  await page.route('**/api/v1/barbearias/b1/dashboard', (rota) =>
    rota.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(dados) }),
  );

  await page.goto('/');
  await page.getByRole('button', { name: 'Criar barbearia' }).click();
  await page.locator('input#barbearia-nome').fill('Barbearia E2E');
  await page.locator('input#barbearia-telefone').fill('(11) 99999-9999');
  await page.locator('input#barbearia-email').fill('contato@exemplo.com');
  await page.locator('input#barbearia-cidade').fill('São Paulo');
  await page.locator('input#barbearia-endereco').fill('Rua Central, 100');
  await page.getByRole('button', { name: 'Criar barbearia', exact: true }).last().click();
  await expect(page.getByRole('heading', { name: 'Barbearia criada' })).toBeVisible();
  await page.getByRole('button', { name: 'Gerenciar profissionais' }).click();
}

test('dashboard é a primeira tela após a configuração inicial', async ({ page }) => {
  await preparar(page);

  await expect(page.getByRole('heading', { name: 'Dashboard', level: 1 })).toBeVisible();
  await expect(page.getByText('Cliente Um')).toBeVisible();
  await expect(page.getByText('Barbeiro Um')).toBeVisible();
  await expect(page.getByText(/1\.250,50/)).toBeVisible();
});

test('indicadores sem dados apresentam zero e estado vazio', async ({ page }) => {
  await preparar(page, { hoje: 0, estaSemana: 0, cancelamentos: 0, recebidoNoMes: 0, proximos: [] });

  await expect(page.getByText('Nenhum agendamento futuro')).toBeVisible();
  await expect(page.getByText(/0,00/)).toBeVisible();
});

test('proprietário acessa o dashboard pela navegação', async ({ page }) => {
  await preparar(page);
  await expect(page.getByRole('heading', { name: 'Dashboard', level: 1 })).toBeVisible();

  await page.getByRole('button', { name: 'Agenda', exact: true }).click();
  await page.getByRole('button', { name: 'Dashboard', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Dashboard', level: 1 })).toBeVisible();
});

test('erro da API é exibido', async ({ page }) => {
  await preparar(page);
  await expect(page.getByRole('heading', { name: 'Dashboard', level: 1 })).toBeVisible();

  await page.route('**/api/v1/barbearias/b1/dashboard', (rota) =>
    rota.fulfill({
      status: 403,
      contentType: 'application/problem+json',
      body: JSON.stringify({ detail: 'Somente o proprietário pode acessar o dashboard.' }),
    }),
  );
  await page.getByRole('button', { name: 'Agenda', exact: true }).click();
  await page.getByRole('button', { name: 'Dashboard', exact: true }).click();

  await expect(page.getByRole('alert')).toContainText('Somente o proprietário');
});

test('axe não encontra violações críticas no dashboard', async ({ page }) => {
  await preparar(page);
  const resultados = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
    .analyze();
  const criticas = resultados.violations.filter((violacao) => violacao.impact === 'critical');
  expect(criticas, criticas.map((violacao) => violacao.help).join('; ')).toEqual([]);
});
