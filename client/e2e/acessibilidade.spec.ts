import AxeBuilder from '@axe-core/playwright';
import { expect, test } from '@playwright/test';

test('página inicial não apresenta violações críticas de acessibilidade', async ({ page }) => {
  await page.goto('/');

  const resultados = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
    .analyze();

  const criticas = resultados.violations.filter((violacao) => violacao.impact === 'critical');
  expect(criticas, criticas.map((violacao) => violacao.help).join('; ')).toEqual([]);
});
