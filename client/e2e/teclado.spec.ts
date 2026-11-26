import { expect, test, type Page } from '@playwright/test';

const conta = {
  id: '33333333-3333-3333-3333-333333333333',
  nomeCompleto: 'Cliente Teclado',
  email: 'cliente-teclado@exemplo.com',
  telefone: '11999999999',
  tipo: 'Customer',
};

async function preparar(page: Page): Promise<void> {
  await page.route('**/api/v1/auth/me', (rota) =>
    rota.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(conta) }),
  );
  await page.route('**/api/v1/notificacoes', (rota) =>
    rota.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ itens: [], naoLidas: 0, pagina: 1, tamanho: 20 }),
    }),
  );
  await page.route('**/api/v1/minha-conta/agendamentos', (rota) =>
    rota.fulfill({ status: 200, contentType: 'application/json', body: '[]' }),
  );
  await page.route('**/api/v1/barbearias/b1/servicos', (rota) =>
    rota.fulfill({ status: 200, contentType: 'application/json', body: '[]' }),
  );
  await page.goto('/');
}

test('navegação por teclado mantém foco visível e nomes acessíveis', async ({ page }) => {
  await preparar(page);

  const botoes = page.locator('nav app-button button');
  const quantidade = await botoes.count();
  expect(quantidade).toBeGreaterThan(2);

  // Todas as actions têm nome acessível (texto visível ou aria-label).
  for (let indice = 0; indice < quantidade; indice++) {
    const botao = botoes.nth(indice);
    const nome = (await botao.innerText()).trim() || (await botao.getAttribute('aria-label'));
    expect(nome, `botão ${indice} sem nome acessível`).toBeTruthy();
  }

  // Tab percorre os controles e o elemento focado possui indicador visível.
  await page.keyboard.press('Tab');
  const primeiroFoco = await page.evaluate(() => document.activeElement?.tagName);
  expect(primeiroFoco).toBeTruthy();

  const visivel = await page.evaluate(() => {
    const elemento = document.activeElement as HTMLElement | null;
    if (!elemento) return false;
    const estilo = getComputedStyle(elemento);
    return estilo.outlineStyle !== 'none' || estilo.boxShadow !== 'none';
  });
  expect(visivel, 'elemento focado sem indicador visual').toBe(true);

  await page.keyboard.press('Tab');
  const segundoFoco = await page.evaluate(() => document.activeElement?.outerHTML ?? '');
  expect(segundoFoco).not.toContain('undefined');
});

test('formulário de perfil é navegável por teclado', async ({ page }) => {
  await preparar(page);
  await page.getByRole('button', { name: 'Minha conta' }).click();

  const campoNome = page.locator('input#perfil-nome');
  await campoNome.fill('');
  await campoNome.focus();
  await page.keyboard.type('Nome Digitado');
  await expect(campoNome).toHaveValue('Nome Digitado');

  await page.keyboard.press('Tab');
  const proximo = await page.evaluate(() => document.activeElement?.id ?? '');
  expect(proximo).toBe('perfil-email');
});
