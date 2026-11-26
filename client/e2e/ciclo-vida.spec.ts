import AxeBuilder from '@axe-core/playwright';
import { expect, test, type Page } from '@playwright/test';

const contaCliente = {
  id: '33333333-3333-3333-3333-333333333333',
  nomeCompleto: 'Cliente E2E',
  email: 'cliente-e2e@exemplo.com',
  telefone: '11999999999',
  tipo: 'Customer',
};

const contaProprietario = {
  id: '11111111-1111-1111-1111-111111111111',
  nomeCompleto: 'Dono E2E',
  email: 'dono-e2e@exemplo.com',
  telefone: '11999999999',
  tipo: 'Professional',
};

const agendamento = {
  id: 'a1',
  barbeariaId: 'b1',
  profissionalId: 'p1',
  servicoId: 's1',
  iniciaEmUtc: '2026-11-23T13:00:00Z',
  terminaEmUtc: '2026-11-23T13:40:00Z',
  nomeServico: 'Corte degradê',
  duracaoMinutos: 40,
  preco: 80,
  situacao: 'Confirmado',
  origem: 'Publico',
};

const historico = [
  {
    id: 'h1',
    agendamentoId: 'a1',
    tipoEvento: 'Criacao',
    atorUsuarioId: null,
    ocorridoEmUtc: '2026-11-20T10:00:00Z',
    inicioAnteriorUtc: null,
    novoInicioUtc: null,
    detalhes: 'Origem: Publico',
  },
  {
    id: 'h2',
    agendamentoId: 'a1',
    tipoEvento: 'Remarcacao',
    atorUsuarioId: 'cliente-1',
    ocorridoEmUtc: '2026-11-21T10:00:00Z',
    inicioAnteriorUtc: '2026-11-23T12:00:00Z',
    novoInicioUtc: '2026-11-23T13:00:00Z',
    detalhes: null,
  },
];

function sugestao(expirada: boolean) {
  return {
    id: 'sg1',
    agendamentoId: 'a1',
    profissionalId: 'p1',
    inicioSugeridoUtc: '2026-11-24T13:00:00Z',
    fimSugeridoUtc: '2026-11-24T13:40:00Z',
    motivo: 'Horário melhor para a agenda',
    situacao: expirada ? 'Expirada' : 'Pendente',
    expiraEmUtc: new Date(Date.now() + (expirada ? -3600_000 : 3600_000)).toISOString(),
    respondidoEmUtc: null,
    autorUsuarioId: 'p1',
  };
}

async function preparar(page: Page, conta: typeof contaCliente, sugestoes: unknown[]): Promise<void> {
  await page.route('**/api/v1/auth/me', (rota) =>
    rota.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(conta) }),
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
        codigoConvite: conta.tipo === 'Professional' ? 'ABC23456' : null,
        fusoHorarioId: 'America/Sao_Paulo',
      }),
    }),
  );
  await page.route('**/api/v1/barbearias/b1/profissionais', (rota) =>
    rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        { id: 'p1', nomeCompleto: 'Dono E2E', telefone: '11999999999', situacao: 'Ativo' },
      ]),
    }),
  );
  await page.route('**/api/v1/barbearias/b1/profissionais/p1/blocks', (rota) =>
    rota.fulfill({ status: 200, contentType: 'application/json', body: '[]' }),
  );
  await page.route(/\/api\/v1\/minha-conta\/agendamentos/, (rota) =>
    rota.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([agendamento]) }),
  );
  await page.route(/\/api\/v1\/barbearias\/b1\/agendamentos\/a1(\/historico|\/sugestoes|\?.*)?$/, (rota) => {
    const url = rota.request().url();
    if (url.endsWith('/historico'))
      return rota.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(historico) });
    if (url.endsWith('/sugestoes') && rota.request().method() === 'GET')
      return rota.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(sugestoes) });
    if (rota.request().method() === 'GET')
      return rota.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(agendamento) });
    return rota.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(agendamento) });
  });
  await page.route(/\/api\/v1\/barbearias\/b1\/agendamentos(\?.*)?$/, (rota) =>
    rota.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([agendamento]) }),
  );
  await page.route(/\/api\/v1\/barbearias\/b1\/disponibilidade/, (rota) =>
    rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        date: '2026-11-24',
        fusoHorarioId: 'America/Sao_Paulo',
        slots: [
          {
            profissionalId: 'p1',
            startsAtUtc: '2026-11-24T13:00:00Z',
            endsAtUtc: '2026-11-24T13:40:00Z',
            startsAtLocal: '2026-11-24 10:00',
          },
        ],
        hasAvailability: true,
      }),
    }),
  );
  await page.goto('/');
  if (conta.tipo === 'Professional') {
    await page.getByRole('button', { name: 'Criar barbearia' }).click();
    await page.locator('input#barbearia-nome').fill('Barbearia E2E');
    await page.locator('input#barbearia-telefone').fill('(11) 99999-9999');
    await page.locator('input#barbearia-email').fill('contato@exemplo.com');
    await page.locator('input#barbearia-cidade').fill('São Paulo');
    await page.locator('input#barbearia-endereco').fill('Rua Central, 100');
    await page.getByRole('button', { name: 'Criar barbearia', exact: true }).last().click();
    await expect(page.getByRole('heading', { name: 'Barbearia criada' })).toBeVisible();
  }
}

async function abrirDetalheCliente(page: Page): Promise<void> {
  await page.getByRole('button', { name: 'Meus agendamentos' }).click();
  await expect(page.getByRole('heading', { name: 'Meus agendamentos' })).toBeVisible();
  await page.getByRole('button', { name: 'Gerenciar' }).click();
  await expect(page.getByRole('heading', { name: 'Detalhes do agendamento' })).toBeVisible();
}

test('cliente cancela com confirmação e vê o resultado', async ({ page }) => {
  await page.route('**/api/v1/barbearias/b1/agendamentos/a1/cancelar', (rota) =>
    rota.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(agendamento) }),
  );
  await preparar(page, contaCliente, []);
  await abrirDetalheCliente(page);

  await page.getByRole('button', { name: 'Cancelar agendamento' }).click();
  await page.locator('input#detalhe-motivo-cancelamento').fill('Desistência');
  await page.getByRole('button', { name: 'Confirmar cancelamento' }).click();

  await expect(page.getByText('Agendamento cancelado.')).toBeVisible();
});

test('cliente aceita uma sugestão pendente', async ({ page }) => {
  await page.route('**/api/v1/barbearias/b1/agendamentos/a1/sugestoes/sg1/aceitar', (rota) =>
    rota.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(sugestao(false)) }),
  );
  await preparar(page, contaCliente, [sugestao(false)]);
  await abrirDetalheCliente(page);

  await expect(page.getByText('Horário melhor para a agenda')).toBeVisible();
  await expect(page.getByText(/Válido até/)).toBeVisible();
  await page.getByRole('button', { name: 'Aceitar' }).click();

  await expect(page.getByText('Sugestão aceita.')).toBeVisible();
});

test('sugestão expirada não oferece aceite', async ({ page }) => {
  await preparar(page, contaCliente, [sugestao(true)]);
  await abrirDetalheCliente(page);

  await expect(page.getByText('Nenhuma sugestão pendente')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Aceitar' })).toHaveCount(0);
});

test('histórico é exibido em ordem cronológica', async ({ page }) => {
  await preparar(page, contaCliente, []);
  await abrirDetalheCliente(page);

  const itens = page.locator('ol > li');
  await expect(itens).toHaveCount(2);
  await expect(itens.nth(0)).toContainText('Criação');
  await expect(itens.nth(1)).toContainText('Remarcação');
});

test('proprietário cria sugestão com motivo obrigatório', async ({ page }) => {
  await page.route('**/api/v1/barbearias/b1/agendamentos/a1/sugestoes', (rota) =>
    rota.fulfill({ status: 201, contentType: 'application/json', body: JSON.stringify(sugestao(false)) }),
  );
  await preparar(page, contaProprietario, []);
  await page.getByRole('button', { name: 'Agenda', exact: true }).click();
  await page.getByRole('button', { name: 'Gerenciar' }).click();

  await expect(page.getByRole('heading', { name: 'Detalhes do agendamento' })).toBeVisible();
  await page.locator('input#detalhe-data').fill('2026-11-24');
  await page.locator('select#detalhe-horario').selectOption('2026-11-24T13:00:00Z');
  await page.locator('input#detalhe-motivo-sugestao').fill('Outro horário livre');
  await page.getByRole('button', { name: 'Sugerir novo horário' }).click();

  await expect(page.getByText('Sugestão enviada ao cliente.')).toBeVisible();
});

test('erro de conflito na remarcação é exibido', async ({ page }) => {
  await page.route('**/api/v1/barbearias/b1/agendamentos/a1/remarcar', (rota) =>
    rota.fulfill({
      status: 409,
      contentType: 'application/problem+json',
      body: JSON.stringify({ code: 'schedule.slot_unavailable', detail: 'O horário não está mais disponível.' }),
    }),
  );
  await preparar(page, contaCliente, []);
  await abrirDetalheCliente(page);

  await page.locator('input#detalhe-data').fill('2026-11-24');
  await page.locator('select#detalhe-horario').selectOption('2026-11-24T13:00:00Z');
  await page.getByRole('button', { name: 'Remarcar' }).click();

  await expect(page.getByRole('alert')).toContainText('já está ocupado');
});

test('axe não encontra violações críticas nas telas do ciclo de vida', async ({ page }) => {
  await preparar(page, contaCliente, [sugestao(false)]);
  await abrirDetalheCliente(page);

  const resultados = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
    .analyze();
  const criticas = resultados.violations.filter((violacao) => violacao.impact === 'critical');
  expect(criticas, criticas.map((violacao) => violacao.help).join('; ')).toEqual([]);
});
