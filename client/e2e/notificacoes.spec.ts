import AxeBuilder from '@axe-core/playwright';
import { expect, test, type Page } from '@playwright/test';

const conta = {
  id: '33333333-3333-3333-3333-333333333333',
  nomeCompleto: 'Cliente E2E',
  email: 'cliente-e2e@exemplo.com',
  telefone: '11999999999',
  tipo: 'Customer',
};

const notificacao = {
  id: 'n1',
  barbeariaId: 'b1',
  tipo: 'AgendamentoCriado',
  agendamentoId: 'a1',
  mensagem: 'Novo agendamento confirmado.',
  criadoEmUtc: '2026-11-23T10:00:00Z',
  lidoEmUtc: null,
};

async function preparar(page: Page, itens: unknown[] = [notificacao], naoLidas = 1): Promise<void> {
  await page.route('**/api/v1/auth/me', (rota) =>
    rota.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(conta) }),
  );
  await page.route('**/api/v1/notificacoes', (rota) => {
    if (rota.request().method() === 'GET') {
      return rota.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ itens, naoLidas, pagina: 1, tamanho: 20 }),
      });
    }
    return rota.fulfill({ status: 200, contentType: 'application/json', body: '{"marcadas":1}' });
  });
  await page.route('**/api/v1/notificacoes/*', (rota) =>
    rota.fulfill({ status: 204, body: '' }),
  );

  await page.goto('/');
  await page.getByRole('button', { name: /Notificações/ }).click();
  await expect(page.getByRole('heading', { name: 'Notificações' })).toBeVisible();
}

test('central exibe notificações, contador e ações', async ({ page }) => {
  await preparar(page);

  await expect(page.getByText('Novo agendamento confirmado.')).toBeVisible();
  await expect(page.getByText('1 não lidas')).toBeVisible();
  await page.getByRole('button', { name: 'Marcar como lida' }).click();
  await expect(page.getByText('Novo agendamento confirmado.')).toBeVisible();
  await page.getByRole('button', { name: 'Marcar todas como lidas' }).click();
  await expect(page.getByText('Novo agendamento confirmado.')).toBeVisible();
});

test('estado vazio é exibido', async ({ page }) => {
  await preparar(page, [], 0);
  await expect(page.getByText('Nenhuma notificação')).toBeVisible();
});

test('erro de carregamento é exibido', async ({ page }) => {
  await page.route('**/api/v1/auth/me', (rota) =>
    rota.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(conta) }),
  );
  await page.route('**/api/v1/notificacoes', (rota) =>
    rota.fulfill({
      status: 500,
      contentType: 'application/problem+json',
      body: JSON.stringify({ detail: 'Falha ao carregar.' }),
    }),
  );

  await page.goto('/');
  await page.getByRole('button', { name: /Notificações/ }).click();

  await expect(page.getByRole('alert')).toContainText('Falha ao carregar.');
});

test('axe não encontra violações críticas na central', async ({ page }) => {
  await preparar(page);
  const resultados = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
    .analyze();
  const criticas = resultados.violations.filter((violacao) => violacao.impact === 'critical');
  expect(criticas, criticas.map((violacao) => violacao.help).join('; ')).toEqual([]);
});
