import AxeBuilder from '@axe-core/playwright';
import { expect, test, type Page } from '@playwright/test';

const contaProfissional = {
  id: '11111111-1111-1111-1111-111111111111',
  nomeCompleto: 'Proprietario E2E',
  email: 'proprietario-e2e@exemplo.com',
  telefone: '11999999999',
  tipo: 'Professional',
};

async function prepararSessao(page: Page): Promise<void> {
  await page.route('**/api/v1/auth/me', (rota) =>
    rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(contaProfissional),
    }),
  );
  await page.goto('/');
  await expect(page.getByRole('button', { name: 'Criar barbearia' })).toBeVisible();
}

test('criação de barbearia exibe o código de convite', async ({ page }) => {
  await prepararSessao(page);

  await page.route('**/api/v1/barbearias', (rota) => {
    if (rota.request().method() === 'POST') {
      return rota.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'b1',
          nome: 'Barbearia E2E',
          telefoneComercial: '(11) 99999-9999',
          emailComercial: 'contato@exemplo.com',
          cidade: 'São Paulo',
          endereco: 'Rua Central, 100',
          instagram: null,
          codigoConvite: 'ABC23456',
          fusoHorarioId: 'America/Sao_Paulo',
        }),
      });
    }
    return rota.continue();
  });

  await page.getByRole('button', { name: 'Criar barbearia' }).click();
  await page.locator('input#barbearia-nome').fill('Barbearia E2E');
  await page.locator('input#barbearia-telefone').fill('(11) 99999-9999');
  await page.locator('input#barbearia-email').fill('contato@exemplo.com');
  await page.locator('input#barbearia-cidade').fill('São Paulo');
  await page.locator('input#barbearia-endereco').fill('Rua Central, 100');
  await page.getByRole('button', { name: 'Criar barbearia', exact: true }).last().click();

  await expect(page.getByRole('heading', { name: 'Barbearia criada' })).toBeVisible();
  await expect(page.getByText('ABC23456')).toBeVisible();
});

test('entrada por código de convite lista os profissionais da barbearia', async ({ page }) => {
  await prepararSessao(page);

  await page.route('**/api/v1/barbearias/entrar', (rota) =>
    rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        id: 'v1',
        barbeariaId: 'b1',
        profissionalId: 'p1',
        cargo: 'Barbeiro',
        situacao: 'Ativo',
      }),
    }),
  );
  await page.route('**/api/v1/barbearias/b1/profissionais', (rota) =>
    rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        {
          id: 'p1',
          nomeCompleto: 'Profissional Convite',
          telefone: '11999999999',
          situacao: 'Ativo',
        },
      ]),
    }),
  );

  await page.getByRole('button', { name: 'Entrar em barbearia' }).click();
  await page.locator('input#convite-codigo').fill('ABC23456');
  await page.getByRole('button', { name: 'Entrar na barbearia' }).click();

  await expect(page.getByText('Você entrou na barbearia com o cargo Barbeiro.')).toBeVisible();
  await page.getByRole('button', { name: 'Ver profissionais' }).click();

  await expect(page.getByRole('heading', { name: 'Profissionais' })).toBeVisible();
  await expect(page.getByText('Profissional Convite')).toBeVisible();
});

test('proprietário desativa um profissional', async ({ page }) => {
  await prepararSessao(page);

  await page.route('**/api/v1/barbearias', (rota) => {
    if (rota.request().method() === 'POST') {
      return rota.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'b1',
          nome: 'Barbearia E2E',
          telefoneComercial: '(11) 99999-9999',
          emailComercial: 'contato@exemplo.com',
          cidade: 'São Paulo',
          endereco: 'Rua Central, 100',
          instagram: null,
          codigoConvite: 'ABC23456',
          fusoHorarioId: 'America/Sao_Paulo',
        }),
      });
    }
    return rota.continue();
  });

  await page.getByRole('button', { name: 'Criar barbearia' }).click();
  await page.locator('input#barbearia-nome').fill('Barbearia E2E');
  await page.locator('input#barbearia-telefone').fill('(11) 99999-9999');
  await page.locator('input#barbearia-email').fill('contato@exemplo.com');
  await page.locator('input#barbearia-cidade').fill('São Paulo');
  await page.locator('input#barbearia-endereco').fill('Rua Central, 100');
  await page.getByRole('button', { name: 'Criar barbearia', exact: true }).last().click();
  await expect(page.getByRole('heading', { name: 'Barbearia criada' })).toBeVisible();

  let situacao = 'Ativo';
  await page.route('**/api/v1/barbearias/b1/profissionais', (rota) =>
    rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        { id: 'p1', nomeCompleto: 'Profissional Alvo', telefone: '11999999999', situacao },
      ]),
    }),
  );
  await page.route('**/api/v1/barbearias/b1/profissionais/p1/status', async (rota) => {
    expect(rota.request().method()).toBe('PATCH');
    expect(rota.request().postDataJSON()).toEqual({ ativo: false });
    situacao = 'Inativo';
    await rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ id: 'p1', nomeCompleto: 'Profissional Alvo', situacao }),
    });
  });

  await page.getByRole('button', { name: 'Profissionais', exact: true }).click();
  const linha = page.locator('tbody tr', { hasText: 'Profissional Alvo' });
  await expect(linha).toContainText('Ativo');

  await linha.getByRole('button', { name: 'Desativar' }).click();

  await expect(linha).toContainText('Inativo');
  await expect(linha.getByRole('button', { name: 'Ativar' })).toBeVisible();
});

test('tela de profissionais não apresenta violações críticas de acessibilidade', async ({
  page,
}) => {
  await prepararSessao(page);
  await page.route('**/api/v1/barbearias/b1/profissionais', (rota) =>
    rota.fulfill({ status: 200, contentType: 'application/json', body: '[]' }),
  );
  await page.getByRole('button', { name: 'Profissionais', exact: true }).click();

  const resultados = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
    .analyze();
  const criticas = resultados.violations.filter((violacao) => violacao.impact === 'critical');
  expect(criticas, criticas.map((violacao) => violacao.help).join('; ')).toEqual([]);
});
