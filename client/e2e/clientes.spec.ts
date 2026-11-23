import AxeBuilder from '@axe-core/playwright';
import { expect, test, type Page } from '@playwright/test';

const contaDono = {
  id: '11111111-1111-1111-1111-111111111111',
  nomeCompleto: 'Dono E2E',
  email: 'dono-e2e@exemplo.com',
  telefone: '11999999999',
  tipo: 'Professional',
};

const contaCliente = {
  id: '33333333-3333-3333-3333-333333333333',
  nomeCompleto: 'Cliente Alvo',
  email: 'cliente-alvo@exemplo.com',
  telefone: '11999999999',
  tipo: 'Customer',
};

const cliente = {
  id: contaCliente.id,
  nomeCompleto: 'Cliente Alvo',
  email: 'cliente-alvo@exemplo.com',
  telefone: '11999999999',
};

async function prepararDono(page: Page, itens: unknown[] = [cliente]): Promise<void> {
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
  await page.route(/\/api\/v1\/barbearias\/b1\/customers/, (rota) => {
    const url = rota.request().url();
    if (url.endsWith(`/customers/${cliente.id}`))
      return rota.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(cliente) });
    return rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ itens, total: itens.length, pagina: 1, tamanho: 20 }),
    });
  });

  await page.goto('/');
  await page.getByRole('button', { name: 'Criar barbearia' }).click();
  await page.locator('input#barbearia-nome').fill('Barbearia E2E');
  await page.locator('input#barbearia-telefone').fill('(11) 99999-9999');
  await page.locator('input#barbearia-email').fill('contato@exemplo.com');
  await page.locator('input#barbearia-cidade').fill('São Paulo');
  await page.locator('input#barbearia-endereco').fill('Rua Central, 100');
  await page.getByRole('button', { name: 'Criar barbearia', exact: true }).last().click();
  await expect(page.getByRole('heading', { name: 'Barbearia criada' })).toBeVisible();
  await page.getByRole('button', { name: 'Clientes', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Clientes' })).toBeVisible();
}

test('proprietário lista, pesquisa e consulta clientes', async ({ page }) => {
  let busca = '';
  page.on('request', (requisicao) => {
    if (requisicao.url().includes('/customers?'))
      busca = new URL(requisicao.url()).searchParams.get('busca') ?? '';
  });
  await prepararDono(page);

  await expect(page.getByText('Cliente Alvo')).toBeVisible();

  await page.locator('input#clientes-busca').fill('Alvo');
  await page.getByRole('button', { name: 'Pesquisar' }).click();
  expect(busca).toBe('Alvo');

  await page.getByRole('button', { name: 'Consultar' }).click();
  await expect(page.getByRole('heading', { name: 'Dados do cliente' })).toBeVisible();
  await expect(page.getByText('Não há ações de cadastro, edição ou exclusão aqui.')).toBeVisible();
});

test('estado vazio é exibido quando não há clientes', async ({ page }) => {
  await prepararDono(page, []);
  await expect(page.getByText('Nenhum cliente encontrado')).toBeVisible();
});

test('não há ações administrativas de cadastro, edição ou exclusão', async ({ page }) => {
  await prepararDono(page);
  await expect(page.getByRole('button', { name: /novo cliente|editar|excluir/i })).toHaveCount(0);
});

test('cliente edita somente os próprios dados', async ({ page }) => {
  await page.route('**/api/v1/auth/me', (rota) =>
    rota.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(contaCliente) }),
  );
  let corpo: Record<string, unknown> | null = null;
  await page.route('**/api/v1/me/customer-profile', (rota) => {
    if (rota.request().method() === 'PUT') corpo = rota.request().postDataJSON();
    return rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ ...cliente, nomeCompleto: 'Cliente Novo' }),
    });
  });

  await page.goto('/');
  await page.getByRole('button', { name: 'Minha conta' }).click();
  await page.locator('input#perfil-nome').fill('Cliente Novo');
  await page.getByRole('button', { name: 'Salvar dados' }).click();

  await expect(page.getByText('Dados atualizados.')).toBeVisible();
  expect(corpo).toEqual({
    nomeCompleto: 'Cliente Novo',
    email: 'cliente-alvo@exemplo.com',
    telefone: '11999999999',
  });
});

test('erro de e-mail duplicado é exibido', async ({ page }) => {
  await page.route('**/api/v1/auth/me', (rota) =>
    rota.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(contaCliente) }),
  );
  await page.route('**/api/v1/me/customer-profile', (rota) =>
    rota.fulfill({
      status: 409,
      contentType: 'application/problem+json',
      body: JSON.stringify({ detail: 'Este e-mail já está em uso.' }),
    }),
  );

  await page.goto('/');
  await page.getByRole('button', { name: 'Minha conta' }).click();
  await page.locator('input#perfil-email').fill('outro@exemplo.com');
  await page.getByRole('button', { name: 'Salvar dados' }).click();

  await expect(page.getByRole('alert')).toContainText('já está em uso');
});

test('axe não encontra violações críticas nas telas de clientes', async ({ page }) => {
  await prepararDono(page);
  const resultados = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
    .analyze();
  const criticas = resultados.violations.filter((violacao) => violacao.impact === 'critical');
  expect(criticas, criticas.map((violacao) => violacao.help).join('; ')).toEqual([]);
});
