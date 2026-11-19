# PontoBarber Web

SPA Angular 22 do PontoBarber (ADR-001). Foundation visual: Bootstrap 5.3 + SCSS + Bootstrap Icons, com importação seletiva e tema por tokens (ADR-026).

O workspace fica diretamente em `client/` e não usa roteamento: os arquivos principais são `src/app/app.ts`, `app.html` e `app.config.ts`. Pastas de funcionalidades e `shared/` em `src/app/` serão criadas quando as tarefas seguintes exigirem.

## Ambiente

- Node.js **22.22.3** (padrão documentado em `.nvmrc`; ver `engines` no `package.json`).
- npm 10.x ou superior.

```powershell
nvm use                 # Node 22.22.3
npm ci                  # instalação reproduzível pelo package-lock.json
npm start               # servidor de desenvolvimento em http://localhost:4200
```

## Build e orçamento

```powershell
npm run build
npm run medir-bundle
npm test
npm run test:coverage
npm run e2e
```

`medir-bundle` usa `zlib` nativo do Node (sem dependências extras) e contabiliza todos os recursos iniciais efetivamente transferidos, referenciados por `index.html` e pelo CSS inicial: HTML, JS, CSS, favicon e as fontes `.woff2` do Bootstrap Icons. JS, CSS e HTML são medidos após gzip (nível 9); `.woff2` e demais recursos já comprimidos entram pelo tamanho original, pois gzip neles não reflete a transferência. Fontes legadas declaradas apenas como fallback (`.woff`, `.ttf`, `.eot`, `.otf`) não são baixadas pelos navegadores alvo e ficam de fora da medição.

Limite documentado (TDD §17.3 e ADR-001): **300 KB gzip** (307.200 bytes).

Os budgets nativos do `angular.json` (`initial`: aviso 400 kB, erro 450 kB) são proteção complementar sobre o tamanho **bruto** de JS + CSS. Foram derivados da primeira medição real: 375,23 kB brutos (190,52 kB JS + 184,71 kB CSS). O `medir-bundle` é a fonte da verdade do limite gzip, que inclui as fontes.

## Estilos

- `src/styles.scss` — entrada global.
- `src/styles/_bootstrap.scss` — importação seletiva (reboot, grid, utilities e estrutura base); componentes entram quando `shared/ui` existir.
- `src/styles/_tokens.scss` — tokens semânticos aliasados aos valores do Bootstrap; nenhuma paleta própria é inventada.
- `src/styles/_icons.scss` — Bootstrap Icons via Sass.
- `src/styles/_base.scss` — regras globais mínimas.

Avisos de depreciação do Sass `@import` são esperados com Bootstrap 5.3 (documentado pelo próprio Bootstrap) e não impedem a compilação.

## Testes

- `npm test` — testes de componente e do cliente HTTP com Vitest + TestBed (cobertura mínima de 70%).
- `npm run test:coverage` — os mesmos testes com relatório de cobertura.
- `npm run e2e` — auditoria axe com Playwright; uma violação crítica reprova a execução.

Os testes de componente ficam ao lado de cada componente em `src/app/shared/ui/**/*.spec.ts` e usam `axe-core` diretamente, sem wrapper. Em jsdom a regra de contraste é desabilitada (sem layout); a auditoria axe completa, com contraste, ocorre no E2E em navegador real.
