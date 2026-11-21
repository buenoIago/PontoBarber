import AxeBuilder from '@axe-core/playwright';
import { expect, test, type Page } from '@playwright/test';

const contaProfissional = {
  id: '11111111-1111-1111-1111-111111111111',
  nomeCompleto: 'Proprietario E2E',
  email: 'proprietario-e2e@exemplo.com',
  telefone: '11999999999',
  tipo: 'Professional',
};

const contaCliente = {
  ...contaProfissional,
  id: '33333333-3333-3333-3333-333333333333',
  tipo: 'Customer',
};

const servicoAtivo = {
  id: 's1',
  barbeariaId: 'b1',
  nome: 'Corte',
  duracaoMinutos: 30,
  preco: 50,
  ativo: true,
  ehModelo: false,
};

async function prepararSessao(page: Page, conta = contaProfissional): Promise<void> {
  await page.route('**/api/v1/auth/me', (rota) =>
    rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(conta),
    }),
  );
  await page.goto('/');
}

async function criarBarbearia(page: Page): Promise<void> {
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
}

async function entrarComoBarbeiro(page: Page): Promise<void> {
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
    rota.fulfill({ status: 200, contentType: 'application/json', body: '[]' }),
  );

  await page.getByRole('button', { name: 'Entrar em barbearia' }).click();
  await page.locator('input#convite-codigo').fill('ABC23456');
  await page.getByRole('button', { name: 'Entrar na barbearia' }).click();
  await expect(page.getByText('Você entrou na barbearia com o cargo Barbeiro.')).toBeVisible();
}

async function abrirCatalogo(page: Page): Promise<void> {
  await page.getByRole('button', { name: 'Serviços', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Catálogo de serviços' })).toBeVisible();
}

test('proprietário visualiza o catálogo e cria um serviço', async ({ page }) => {
  await prepararSessao(page);
  await criarBarbearia(page);

  let servicos = [servicoAtivo];
  await page.route('**/api/v1/barbearias/b1/servicos', (rota) => {
    if (rota.request().method() === 'POST') {
      const corpo = rota.request().postDataJSON() as {
        nome: string;
        duracaoMinutos: number;
        preco: number;
      };
      const novo = { ...servicoAtivo, id: 's2', ...corpo };
      servicos = [...servicos, novo];
      return rota.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify(novo),
      });
    }
    return rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(servicos),
    });
  });

  await abrirCatalogo(page);
  await expect(page.getByText('Corte', { exact: true })).toBeVisible();

  await page.getByRole('button', { name: 'Novo serviço' }).click();
  await page.locator('input#servico-nome').fill('Barba');
  await page.locator('input#servico-duracao').fill('20');
  await page.locator('input#servico-preco').fill('35');
  await page.getByRole('button', { name: 'Salvar' }).click();

  await expect(page.getByText('Serviço salvo com sucesso.')).toBeVisible();
  await expect(page.getByText('Barba', { exact: true })).toBeVisible();
});

test('proprietário edita os dados e remove logicamente um serviço', async ({ page }) => {
  await prepararSessao(page);
  await criarBarbearia(page);

  let servicos = [
    { ...servicoAtivo },
    { ...servicoAtivo, id: 's2', nome: 'Barba', duracaoMinutos: 20, preco: 35 },
  ];
  await page.route('**/api/v1/barbearias/b1/servicos', (rota) =>
    rota.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(servicos) }),
  );
  await page.route('**/api/v1/barbearias/b1/servicos/s1', (rota) => {
    const corpo = rota.request().postDataJSON() as {
      nome: string;
      duracaoMinutos: number;
      preco: number;
    };
    servicos = servicos.map((item) => (item.id === 's1' ? { ...item, ...corpo } : item));
    return rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(servicos[0]),
    });
  });
  await page.route('**/api/v1/barbearias/b1/servicos/s2', (rota) => {
    servicos = servicos.map((item) => (item.id === 's2' ? { ...item, ativo: false } : item));
    return rota.fulfill({ status: 204 });
  });

  await abrirCatalogo(page);
  await page
    .locator('tbody tr', { hasText: 'Corte' })
    .getByRole('button', { name: 'Editar', exact: true })
    .click();
  await page.locator('[id="editar-servico-nome-s1"]').fill('Corte premium');
  await page.locator('[id="editar-servico-preco-s1"]').fill('90');
  await page.getByRole('button', { name: 'Salvar' }).click();

  await expect(page.getByText('Serviço salvo com sucesso.')).toBeVisible();
  await expect(page.getByText('Corte premium', { exact: true })).toBeVisible();

  const linhaBarba = page.locator('tbody tr', { hasText: 'Barba' });
  await linhaBarba.getByRole('button', { name: 'Remover' }).click();

  await expect(linhaBarba).toContainText('Inativo');
  await expect(page.getByText('Serviço removido da oferta.')).toBeVisible();
  await expect(linhaBarba.getByRole('button', { name: 'Remover' })).toHaveCount(0);
});

test('barbeiro visualiza o catálogo e altera somente preço e duração', async ({ page }) => {
  await prepararSessao(page);
  await entrarComoBarbeiro(page);

  let servicos = [{ ...servicoAtivo }];
  await page.route('**/api/v1/barbearias/b1/servicos', (rota) =>
    rota.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(servicos) }),
  );
  await page.route('**/api/v1/barbearias/b1/servicos/s1', (rota) => {
    const corpo = rota.request().postDataJSON() as { duracaoMinutos: number; preco: number };
    servicos = [{ ...servicoAtivo, ...corpo }];
    return rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(servicos[0]),
    });
  });

  await abrirCatalogo(page);
  await expect(page.getByText('Corte', { exact: true })).toBeVisible();

  await page.getByRole('button', { name: 'Editar valores' }).click();
  await expect(page.locator('[id="editar-servico-nome-s1"]')).toHaveCount(0);
  await expect(page.locator('[id="editar-servico-ativo-s1"]')).toHaveCount(0);
  await page.locator('[id="editar-servico-duracao-s1"]').fill('40');
  await page.locator('[id="editar-servico-preco-s1"]').fill('65');
  await page.getByRole('button', { name: 'Salvar' }).click();

  await expect(page.getByText('40 min', { exact: true })).toBeVisible();
  await expect(page.getByText('R$ 65.00', { exact: true })).toBeVisible();
});

test('barbeiro não recebe opções de nome, disponibilidade ou remoção', async ({ page }) => {
  await prepararSessao(page);
  await entrarComoBarbeiro(page);
  await page.route('**/api/v1/barbearias/b1/servicos', (rota) =>
    rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([servicoAtivo]),
    }),
  );

  await abrirCatalogo(page);

  await expect(page.getByRole('button', { name: 'Novo serviço' })).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Remover' })).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Editar valores' })).toBeVisible();
});

test('cliente visualiza somente serviços ativos e sem ações de edição', async ({ page }) => {
  await prepararSessao(page, contaCliente);
  await page.route('**/api/v1/barbearias/b1/servicos', (rota) =>
    rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([servicoAtivo]),
    }),
  );

  await abrirCatalogo(page);
  await page.locator('input#barbearia-id').fill('b1');
  await page.getByRole('button', { name: 'Consultar catálogo' }).click();

  await expect(page.getByText('Corte', { exact: true })).toBeVisible();
  await expect(page.getByText('Ativo', { exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Novo serviço' })).toHaveCount(0);
  await expect(page.getByRole('button', { name: /Editar/ })).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Remover' })).toHaveCount(0);

  const resultados = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
    .analyze();
  const criticas = resultados.violations.filter((violacao) => violacao.impact === 'critical');
  expect(criticas, criticas.map((violacao) => violacao.help).join('; ')).toEqual([]);
});
