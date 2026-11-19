import axe from 'axe-core';

/**
 * Executa axe apenas no elemento informado. Em jsdom não há layout, então a
 * regra de contraste de cores é desabilitada e verificada na suíte E2E.
 */
export async function violacoesCriticas(elemento: Element): Promise<axe.Result[]> {
  const resultados = await axe.run(elemento, {
    rules: { 'color-contrast': { enabled: false } },
  });

  return resultados.violations.filter((violacao) => violacao.impact === 'critical');
}
