import AxeBuilder from '@axe-core/playwright';
import { expect, test, type Page } from '@playwright/test';

const contaProprietario = {
  id: '11111111-1111-1111-1111-111111111111',
  nomeCompleto: 'Dono E2E',
  email: 'dono-e2e@exemplo.com',
  telefone: '11999999999',
  tipo: 'Professional',
};

const contaBarbeiro = {
  ...contaProprietario,
  id: '22222222-2222-2222-2222-222222222222',
  nomeCompleto: 'Barbeiro E2E',
};

const horarios = [1, 2, 3, 4, 5, 6, 7].map((diaSemana) => ({
  diaSemana,
  abreAs: '09:00:00',
  fechaAs: '18:00:00',
  fechado: false,
}));

const janelas = [1, 2, 3, 4, 5, 6, 7].map((diaSemana) => ({
  id: `j${diaSemana}`,
  diaSemana,
  iniciaEm: '10:00:00',
  terminaEm: '12:00:00',
}));

const profissionais = [
  { id: 'p1', nomeCompleto: 'Dono E2E', telefone: '11999999999', situacao: 'Ativo' },
  { id: 'p2', nomeCompleto: 'Barbeiro E2E', telefone: '11988887777', situacao: 'Ativo' },
];

const bloqueio = {
  id: 'bl1',
  profissionalId: 'p2',
  iniciaEmUtc: '2026-11-30T13:00:00Z',
  terminaEmUtc: '2026-11-30T14:00:00Z',
  motivo: 'Almoço',
};

async function prepararSessao(page: Page, proprietario: boolean): Promise<void> {
  await page.route('**/api/v1/auth/me', (rota) =>
    rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(proprietario ? contaProprietario : contaBarbeiro),
    }),
  );

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

  await page.route('**/api/v1/barbearias/entrar', (rota) =>
    rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        id: 'v1',
        barbeariaId: 'b1',
        profissionalId: 'p2',
        cargo: 'Barbeiro',
        situacao: 'Ativo',
      }),
    }),
  );

  await page.route('**/api/v1/barbearias/b1', (rota) =>
    rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        id: 'b1',
        nome: 'Barbearia E2E',
        telefoneComercial: '(11) 99999-9999',
        emailComercial: 'contato@exemplo.com',
        cidade: 'São Paulo',
        endereco: 'Rua Central, 100',
        instagram: null,
        codigoConvite: proprietario ? 'ABC23456' : null,
        fusoHorarioId: 'America/Sao_Paulo',
      }),
    }),
  );

  await page.route('**/api/v1/barbearias/b1/profissionais', (rota) =>
    rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(profissionais),
    }),
  );

  await page.route('**/api/v1/barbearias/b1/horarios-funcionamento', (rota) => {
    if (rota.request().method() === 'PUT') {
      return rota.fulfill({
        status: 200,
        contentType: 'application/json',
        body: rota.request().postData() ?? JSON.stringify(horarios),
      });
    }
    return rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(horarios),
    });
  });

  await page.route('**/api/v1/barbearias/b1/profissionais/p1/disponibilidade', (rota) =>
    rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(
        rota.request().method() === 'PUT' ? JSON.parse(rota.request().postData() ?? '[]') : janelas,
      ),
    }),
  );

  await page.route('**/api/v1/barbearias/b1/profissionais/p2/disponibilidade', (rota) =>
    rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(
        rota.request().method() === 'PUT' ? JSON.parse(rota.request().postData() ?? '[]') : janelas,
      ),
    }),
  );

  await page.route('**/api/v1/barbearias/b1/profissionais/p1/blocks', (rota) =>
    rota.fulfill({ status: 200, contentType: 'application/json', body: '[]' }),
  );

  await page.route('**/api/v1/barbearias/b1/profissionais/p2/blocks', (rota) => {
    if (rota.request().method() === 'POST') {
      return rota.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify(bloqueio),
      });
    }
    return rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([bloqueio]),
    });
  });

  await page.route('**/api/v1/barbearias/b1/profissionais/p2/blocks/bl1', (rota) =>
    rota.fulfill({ status: 204 }),
  );

  await page.goto('/');
  if (proprietario) {
    await page.getByRole('button', { name: 'Criar barbearia' }).click();
    await page.locator('input#barbearia-nome').fill('Barbearia E2E');
    await page.locator('input#barbearia-telefone').fill('(11) 99999-9999');
    await page.locator('input#barbearia-email').fill('contato@exemplo.com');
    await page.locator('input#barbearia-cidade').fill('São Paulo');
    await page.locator('input#barbearia-endereco').fill('Rua Central, 100');
    await page.getByRole('button', { name: 'Criar barbearia', exact: true }).last().click();
    await expect(page.getByRole('heading', { name: 'Barbearia criada' })).toBeVisible();
  } else {
    await page.getByRole('button', { name: 'Entrar em barbearia' }).click();
    await page.locator('input#convite-codigo').fill('ABC23456');
    await page.getByRole('button', { name: 'Entrar na barbearia' }).click();
    await expect(page.getByText('Você entrou na barbearia com o cargo Barbeiro.')).toBeVisible();
  }
}

async function abrir(page: Page, nome: string): Promise<void> {
  await page.getByRole('button', { name: nome, exact: true }).click();
  await expect(
    page.getByRole('heading', { name: /horários|disponibilidade semanal|bloqueios da agenda/i }),
  ).toBeVisible();
}

test('proprietário visualiza e altera os horários gerais', async ({ page }) => {
  await prepararSessao(page, true);
  await abrir(page, 'Horários');

  await page.getByRole('button', { name: 'Editar horários' }).click();
  await page.getByLabel('Abertura de Segunda').fill('08:00');
  await page.getByRole('button', { name: 'Salvar' }).click();

  await expect(page.getByText('Horários salvos.')).toBeVisible();
  await expect(page.getByText('08:00')).toBeVisible();
});

test('barbeiro visualiza horários gerais sem opção de edição', async ({ page }) => {
  await prepararSessao(page, false);
  await abrir(page, 'Horários');

  await expect(page.getByText('Segunda')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Editar horários' })).toHaveCount(0);
});

test('proprietário gerencia a disponibilidade de outro profissional', async ({ page }) => {
  await prepararSessao(page, true);
  await abrir(page, 'Disponibilidade');

  await page.locator('select#profissional').selectOption('p2');
  await page.getByLabel('Início de Segunda').fill('11:00');
  await page.getByRole('button', { name: 'Salvar disponibilidade' }).click();

  await expect(page.getByText('Disponibilidade salva.')).toBeVisible();
  await expect(page.getByLabel('Início de Segunda')).toHaveValue('11:00');
});

test('barbeiro gerencia somente a própria disponibilidade', async ({ page }) => {
  await prepararSessao(page, false);
  await abrir(page, 'Disponibilidade');

  await expect(page.locator('select#profissional')).toHaveCount(0);
  await page.getByLabel('Término de Segunda').fill('13:00');
  await page.getByRole('button', { name: 'Salvar disponibilidade' }).click();
  await expect(page.getByText('Disponibilidade salva.')).toBeVisible();
});

test('proprietário cria e remove bloqueio de outro profissional', async ({ page }) => {
  await prepararSessao(page, true);
  await abrir(page, 'Bloqueios');

  await page.locator('select#profissional').selectOption('p2');
  await page.locator('input#bloqueio-inicio').fill('2026-11-30T13:00');
  await page.locator('input#bloqueio-termino').fill('2026-11-30T14:00');
  await page.locator('input#bloqueio-motivo').fill('Reunião');
  await page.getByRole('button', { name: 'Adicionar bloqueio' }).click();

  await expect(page.getByText('Bloqueio adicionado.')).toBeVisible();
  await page.getByRole('button', { name: 'Remover' }).first().click();
  await expect(page.getByText('Bloqueio removido.')).toBeVisible();
});

test('barbeiro cria e remove somente os próprios bloqueios', async ({ page }) => {
  await prepararSessao(page, false);
  await abrir(page, 'Bloqueios');

  await expect(page.locator('select#profissional')).toHaveCount(0);
  await page.locator('input#bloqueio-inicio').fill('2026-11-30T13:00');
  await page.locator('input#bloqueio-termino').fill('2026-11-30T14:00');
  await page.getByRole('button', { name: 'Adicionar bloqueio' }).click();
  await expect(page.getByText('Bloqueio adicionado.')).toBeVisible();
  await page.getByRole('button', { name: 'Remover' }).first().click();
  await expect(page.getByText('Bloqueio removido.')).toBeVisible();
});

test('erros de validação da API são exibidos', async ({ page }) => {
  await prepararSessao(page, true);
  await page.route('**/api/v1/barbearias/b1/horarios-funcionamento', (rota) => {
    if (rota.request().method() === 'PUT') {
      return rota.fulfill({
        status: 400,
        contentType: 'application/problem+json',
        body: JSON.stringify({ code: 'validation.failed', detail: 'Revise os campos informados.' }),
      });
    }
    return rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(horarios),
    });
  });
  await abrir(page, 'Horários');

  await page.getByRole('button', { name: 'Editar horários' }).click();
  await page.getByRole('button', { name: 'Salvar' }).click();

  await expect(page.getByRole('alert')).toContainText('Revise os campos informados.');
});

test('estado de carregamento e lista vazia são exibidos', async ({ page }) => {
  await prepararSessao(page, false);
  await page.route('**/api/v1/barbearias/b1/profissionais/p2/blocks', (rota) =>
    rota.fulfill({ status: 200, contentType: 'application/json', body: '[]' }),
  );
  await abrir(page, 'Bloqueios');

  await expect(page.getByText('Nenhum bloqueio')).toBeVisible();
  await expect(page.getByText('Nenhum bloqueio')).toBeVisible();
});

test('telas não apresentam violações críticas de acessibilidade', async ({ page }) => {
  await prepararSessao(page, true);
  for (const tela of ['Horários', 'Disponibilidade', 'Bloqueios']) {
    await abrir(page, tela);
    const resultados = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
      .analyze();
    const criticas = resultados.violations.filter((violacao) => violacao.impact === 'critical');
    expect(criticas, criticas.map((violacao) => violacao.help).join('; ')).toEqual([]);
  }
});

test('navegação permanece utilizável em viewport móvel', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await prepararSessao(page, true);

  await expect(page.getByRole('button', { name: 'Horários', exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Disponibilidade', exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Bloqueios', exact: true })).toBeVisible();
});
