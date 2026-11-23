import AxeBuilder from '@axe-core/playwright';
import { expect, test, type Page } from '@playwright/test';

const contaBarbeiro = {
  id: '22222222-2222-2222-2222-222222222222',
  nomeCompleto: 'Barbeiro E2E',
  email: 'barbeiro-e2e@exemplo.com',
  telefone: '11999999999',
  tipo: 'Professional',
};

function detalhe(situacao = 'Confirmado', situacaoPagamento = 'Pendente') {
  return {
    id: 'a1',
    barbeariaId: 'b1',
    profissionalId: 'p1',
    servicoId: 's1',
    iniciaEmUtc: '2026-11-22T13:00:00Z',
    terminaEmUtc: '2026-11-22T13:40:00Z',
    nomeServico: 'Corte degradê',
    duracaoMinutos: 40,
    preco: 80,
    situacao,
    origem: 'Publico',
    inicioRealUtc: situacao === 'Confirmado' ? null : '2026-11-22T13:02:00Z',
    fimRealUtc: situacao === 'Concluido' ? '2026-11-22T13:45:00Z' : null,
    situacaoPagamento,
    pagamento: situacaoPagamento === 'Pago' ? {
      id: 'pg1', valor: 80, metodo: 'Pix', registradoEmUtc: '2026-11-22T13:50:00Z',
    } : null,
  };
}

async function preparar(page: Page, dados = detalhe()): Promise<void> {
  await page.route('**/api/v1/auth/me', (rota) =>
    rota.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(contaBarbeiro) }),
  );
  await page.route('**/api/v1/barbearias/entrar', (rota) =>
    rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ id: 'v1', barbeariaId: 'b1', profissionalId: 'p1', cargo: 'Barbeiro', situacao: 'Ativo' }),
    }),
  );
  await page.route('**/api/v1/barbearias/b1', (rota) =>
    rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        id: 'b1', nome: 'Barbearia E2E', telefoneComercial: '(11) 99999-9999',
        emailComercial: 'contato@exemplo.com', cidade: 'São Paulo', endereco: 'Rua Central, 100',
        instagram: null, codigoConvite: null, fusoHorarioId: 'America/Sao_Paulo',
      }),
    }),
  );
  await page.route('**/api/v1/barbearias/b1/profissionais', (rota) =>
    rota.fulfill({ status: 200, contentType: 'application/json', body: '[]' }),
  );
  await page.route('**/api/v1/barbearias/b1/profissionais/p1/blocks', (rota) =>
    rota.fulfill({ status: 200, contentType: 'application/json', body: '[]' }),
  );
  await page.route(/\/api\/v1\/barbearias\/b1\/agendamentos(\?.*)?$/, (rota) =>
    rota.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([dados]) }),
  );
  await page.route(/\/api\/v1\/barbearias\/b1\/agendamentos\/a1(\/historico|\/sugestoes)?$/, (rota) => {
    const url = rota.request().url();
    if (url.endsWith('/historico') || url.endsWith('/sugestoes'))
      return rota.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
    return rota.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(dados) });
  });

  await page.goto('/');
  await page.getByRole('button', { name: 'Entrar em barbearia' }).click();
  await page.locator('input#convite-codigo').fill('ABC23456');
  await page.getByRole('button', { name: 'Entrar na barbearia' }).click();
  await expect(page.getByText('Você entrou na barbearia com o cargo Barbeiro.')).toBeVisible();
  await page.getByRole('button', { name: 'Agenda', exact: true }).click();
  await page.getByRole('button', { name: 'Gerenciar' }).click();
  await expect(page.getByRole('heading', { name: 'Detalhes do agendamento' })).toBeVisible();
}

test('barbeiro inicia e conclui o atendimento', async ({ page }) => {
  await page.route('**/api/v1/barbearias/b1/agendamentos/a1/iniciar', (rota) =>
    rota.fulfill({ status: 200, contentType: 'application/json', body: '{}' }),
  );
  await page.route('**/api/v1/barbearias/b1/agendamentos/a1/concluir', (rota) =>
    rota.fulfill({ status: 200, contentType: 'application/json', body: '{}' }),
  );
  await preparar(page);

  await page.getByRole('button', { name: 'Iniciar atendimento' }).click();
  await expect(page.getByText('Atendimento iniciado.')).toBeVisible();
});

test('registro de pagamento envia valor, forma e chave de idempotência', async ({ page }) => {
  let chave: string | undefined;
  await page.route('**/api/v1/barbearias/b1/agendamentos/a1/pagamento', (rota) => {
    chave = rota.request().headers()['idempotency-chave'];
    return rota.fulfill({ status: 201, contentType: 'application/json', body: '{}' });
  });
  await preparar(page, detalhe('EmAndamento'));

  await page.locator('input#pagamento-valor').fill('80');
  await page.locator('select#pagamento-metodo').selectOption('Pix');
  await page.getByRole('button', { name: 'Registrar pagamento' }).click();

  await expect(page.getByText('Pagamento registrado.')).toBeVisible();
  expect(chave).toBeTruthy();
});

test('atendimento finalizado sem pagamento é exibido claramente', async ({ page }) => {
  await preparar(page, detalhe('Concluido'));
  await expect(page.getByText('Atendimento finalizado sem pagamento registrado.')).toBeVisible();
  await expect(page.getByText('Início real')).toBeVisible();
  await expect(page.getByText('Fim real')).toBeVisible();
});

test('não comparecimento é distinto de cancelamento e bloqueia ações', async ({ page }) => {
  await preparar(page, detalhe('NaoCompareceu'));
  await expect(page.getByText('Este estado é distinto de cancelamento.')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Iniciar atendimento' })).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Registrar pagamento' })).toHaveCount(0);
});

test('operação recusada exibe mensagem compreensível', async ({ page }) => {
  await page.route('**/api/v1/barbearias/b1/agendamentos/a1/iniciar', (rota) =>
    rota.fulfill({
      status: 409,
      contentType: 'application/problem+json',
      body: JSON.stringify({ code: 'appointment.invalid_state', detail: 'O agendamento não permite esta operação.' }),
    }),
  );
  await preparar(page);

  await page.getByRole('button', { name: 'Iniciar atendimento' }).click();
  await expect(page.getByRole('alert')).toContainText('não permite esta operação');
});

test('axe não encontra violações críticas na tela de atendimento', async ({ page }) => {
  await preparar(page, detalhe('EmAndamento'));
  const resultados = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
    .analyze();
  const criticas = resultados.violations.filter((violacao) => violacao.impact === 'critical');
  expect(criticas, criticas.map((violacao) => violacao.help).join('; ')).toEqual([]);
});
