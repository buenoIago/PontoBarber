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

const contaCliente = {
  ...contaProprietario,
  id: '33333333-3333-3333-3333-333333333333',
  tipo: 'Customer',
};

const servicos = [{ id: 's1', nome: 'Corte degradê', duracaoMinutos: 40, preco: 80, ativo: true }];

const profissionais = [
  { id: 'p1', nomeCompleto: 'Dono E2E', telefone: '11999999999', situacao: 'Ativo' },
  { id: 'p2', nomeCompleto: 'Barbeiro E2E', telefone: '11988887777', situacao: 'Ativo' },
];

const agendamento = {
  id: 'a1',
  barbeariaId: 'b1',
  profissionalId: 'p2',
  servicoId: 's1',
  iniciaEmUtc: '2026-11-23T13:00:00Z',
  terminaEmUtc: '2026-11-23T13:40:00Z',
  nomeServico: 'Corte degradê',
  duracaoMinutos: 40,
  preco: 80,
  situacao: 'Confirmado',
  origem: 'Publico',
};

const slot = {
  profissionalId: 'p2',
  startsAtUtc: '2026-11-23T14:00:00Z',
  endsAtUtc: '2026-11-23T14:40:00Z',
  startsAtLocal: '2026-11-23 11:00',
};

type Modo = 'criar' | 'entrar' | 'publico';

async function preparar(page: Page, conta: typeof contaProprietario, modo: Modo): Promise<void> {
  await page.route('**/api/v1/auth/me', (rota) =>
    rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(conta),
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
        codigoConvite: modo === 'criar' ? 'ABC23456' : null,
        fusoHorarioId: 'America/Sao_Paulo',
      }),
    }),
  );

  await page.route('**/api/v1/barbearias/b1/servicos', (rota) =>
    rota.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(servicos) }),
  );
  await page.route('**/api/v1/barbearias/b1/profissionais', (rota) =>
    rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(profissionais),
    }),
  );
  await page.route('**/api/v1/barbearias/b1/agendamento-publico', (rota) =>
    rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        barbeariaId: 'b1',
        nomeBarbearia: 'Barbearia E2E',
        fusoHorarioId: 'America/Sao_Paulo',
        profissionais,
      }),
    }),
  );
  await page.route(/\/api\/v1\/barbearias\/b1\/profissionais\/p[12]\/blocks/, (rota) =>
    rota.fulfill({ status: 200, contentType: 'application/json', body: '[]' }),
  );

  const horarios = [1, 2, 3, 4, 5, 6, 7].map((diaSemana) => ({
    diaSemana,
    abreAs: '09:00:00',
    fechaAs: '18:00:00',
    fechado: false,
  }));
  await page.route('**/api/v1/barbearias/b1/horarios-funcionamento', (rota) =>
    rota.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(horarios) }),
  );
  await page.route(/\/api\/v1\/barbearias\/b1\/profissionais\/p[12]\/disponibilidade/, (rota) =>
    rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(
        [1, 2, 3, 4, 5, 6, 7].map((diaSemana) => ({
          id: `j${diaSemana}`,
          diaSemana,
          iniciaEm: '10:00:00',
          terminaEm: '12:00:00',
        })),
      ),
    }),
  );

  await page.route('**/api/v1/barbearias/b1/agendamentos/public', (rota) =>
    rota.fulfill({
      status: 201,
      contentType: 'application/json',
      body: JSON.stringify(agendamento),
    }),
  );
  await page.route(/\/api\/v1\/barbearias\/b1\/agendamentos(\?.*)?$/, (rota) => {
    if (rota.request().method() === 'POST') {
      return rota.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify(agendamento),
      });
    }
    return rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([agendamento]),
    });
  });
  await page.route('**/api/v1/me/agendamentos', (rota) =>
    rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([agendamento]),
    }),
  );
  await page.route(/\/api\/v1\/barbearias\/b1\/disponibilidade/, (rota) =>
    rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        date: '2026-11-23',
        fusoHorarioId: 'America/Sao_Paulo',
        slots: [slot],
        hasAvailability: true,
      }),
    }),
  );

  await page.goto('/');
  if (modo === 'criar') {
    await page.getByRole('button', { name: 'Criar barbearia' }).click();
    await page.locator('input#barbearia-nome').fill('Barbearia E2E');
    await page.locator('input#barbearia-telefone').fill('(11) 99999-9999');
    await page.locator('input#barbearia-email').fill('contato@exemplo.com');
    await page.locator('input#barbearia-cidade').fill('São Paulo');
    await page.locator('input#barbearia-endereco').fill('Rua Central, 100');
    await page.getByRole('button', { name: 'Criar barbearia', exact: true }).last().click();
    await expect(page.getByRole('heading', { name: 'Barbearia criada' })).toBeVisible();
  } else if (modo === 'entrar') {
    await page.getByRole('button', { name: 'Entrar em barbearia' }).click();
    await page.locator('input#convite-codigo').fill('ABC23456');
    await page.getByRole('button', { name: 'Entrar na barbearia' }).click();
    await expect(page.getByText('Você entrou na barbearia com o cargo Barbeiro.')).toBeVisible();
  } else {
    await page.getByRole('button', { name: 'Agendar', exact: true }).click();
    await page.locator('input#barbearia-agendamento').fill('b1');
    await page.getByRole('button', { name: 'Consultar barbearia' }).click();
    await expect(page.locator('select#servico-agendamento')).toBeVisible();
  }
}

test('proprietário visualiza a agenda e alterna entre dia, semana e mês', async ({ page }) => {
  await preparar(page, contaProprietario, 'criar');
  await page.getByRole('button', { name: 'Agenda', exact: true }).click();

  await expect(page.getByText('Corte degradê')).toBeVisible();
  await page.getByRole('button', { name: 'Semana', exact: true }).click();
  await page.getByRole('button', { name: 'Mês', exact: true }).click();
  await page.getByRole('button', { name: 'Próximo período' }).click();
  await expect(page.getByRole('heading', { name: 'Agenda' })).toBeVisible();
});

test('proprietário filtra a agenda por profissional', async ({ page }) => {
  await preparar(page, contaProprietario, 'criar');
  await page.getByRole('button', { name: 'Agenda', exact: true }).click();

  await page.locator('select#profissional-agenda').selectOption('p2');
  await expect(page.getByText('Corte degradê')).toBeVisible();
});

test('barbeiro visualiza somente a própria agenda sem filtro', async ({ page }) => {
  await preparar(page, contaBarbeiro, 'entrar');
  await page.getByRole('button', { name: 'Agenda', exact: true }).click();

  await expect(page.locator('select#profissional-agenda')).toHaveCount(0);
  await expect(page.getByText('Corte degradê')).toBeVisible();
});

test('cliente completa o fluxo público de agendamento', async ({ page }) => {
  await preparar(page, contaCliente, 'publico');

  await page.locator('select#servico-agendamento').selectOption('s1');
  await page.locator('select#profissional-agendamento').selectOption('p2');
  await page.locator('input#data-agendamento').fill('2026-11-23');
  await page.locator('select#horario-agendamento').selectOption(slot.startsAtUtc);
  await page.getByRole('button', { name: 'Confirmar agendamento' }).click();

  await expect(page.getByText('Agendamento confirmado.')).toBeVisible();
});

test('erro de conflito é exibido de forma compreensível', async ({ page }) => {
  await preparar(page, contaCliente, 'publico');
  await page.route('**/api/v1/barbearias/b1/agendamentos/public', (rota) =>
    rota.fulfill({
      status: 409,
      contentType: 'application/problem+json',
      body: JSON.stringify({
        code: 'schedule.slot_unavailable',
        detail: 'O horário não está mais disponível.',
      }),
    }),
  );

  await page.locator('select#servico-agendamento').selectOption('s1');
  await page.locator('select#profissional-agendamento').selectOption('p2');
  await page.locator('input#data-agendamento').fill('2026-11-23');
  await page.locator('select#horario-agendamento').selectOption(slot.startsAtUtc);
  await page.getByRole('button', { name: 'Confirmar agendamento' }).click();

  await expect(page.getByRole('alert')).toContainText('já está ocupado');
});

test('estado vazio é exibido quando não há compromissos', async ({ page }) => {
  await preparar(page, contaProprietario, 'criar');
  await page.route(/\/api\/v1\/barbearias\/b1\/agendamentos(\?.*)?$/, (rota) =>
    rota.fulfill({ status: 200, contentType: 'application/json', body: '[]' }),
  );
  await page.getByRole('button', { name: 'Agenda', exact: true }).click();

  await expect(page.getByText('Nenhum compromisso')).toBeVisible();
});

test('axe não encontra violações críticas na agenda', async ({ page }) => {
  await preparar(page, contaProprietario, 'criar');
  await page.getByRole('button', { name: 'Agenda', exact: true }).click();

  const resultados = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
    .analyze();
  const criticas = resultados.violations.filter((violacao) => violacao.impact === 'critical');
  expect(criticas, criticas.map((violacao) => violacao.help).join('; ')).toEqual([]);
});
