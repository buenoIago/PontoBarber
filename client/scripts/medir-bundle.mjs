// Medição do bundle inicial (TDD §17.3 / ADR-001: ≤ 300 KB gzip).
// Usa apenas zlib nativo do Node — nenhuma dependência adicional.
//
// Regras:
// - JS, CSS e HTML são medidos após gzip (nível 9).
// - .woff2 e demais recursos já comprimidos (imagens, fontes, mídia) entram
//   pelo tamanho original: aplicar gzip neles distorceria a transferência.
// - Fontes legadas declaradas apenas como fallback (.woff, .ttf, .eot, .otf)
//   não são transferidas pelos navegadores alvo e não são contabilizadas.
// - Considera todos os recursos iniciais efetivamente referenciados por
//   index.html e pelo CSS inicial.

import { existsSync, readFileSync, statSync } from 'node:fs';
import { extname, isAbsolute, join, relative, resolve } from 'node:path';
import { gzipSync } from 'node:zlib';

const LIMITE_GZIP = 300 * 1024; // 300 KB (307.200 bytes)
const browserDir = resolve(process.argv[2] ?? 'dist/ponto-barber-web/browser');
const indice = join(browserDir, 'index.html');

const JA_COMPRIMIDOS = new Set([
  '.woff2',
  '.png',
  '.jpg',
  '.jpeg',
  '.gif',
  '.webp',
  '.avif',
  '.ico',
  '.svgz',
  '.mp4',
]);
const FALLBACK_NAO_TRANSFERIDO = new Set(['.woff', '.ttf', '.eot', '.otf']);

if (!existsSync(indice)) {
  console.error(`Build não encontrado em ${browserDir}. Execute "npm run build" primeiro.`);
  process.exit(1);
}

const arquivos = new Set([indice]);
const html = readFileSync(indice, 'utf8');

function resolverLocal(valor) {
  if (!valor || /^(?:data:|https?:|#)/i.test(valor)) return null;
  const limpo = valor.split(/[?#]/)[0];
  const caminho = isAbsolute(limpo) ? join(browserDir, limpo.slice(1)) : resolve(browserDir, limpo);
  if (!existsSync(caminho) || !statSync(caminho).isFile()) return null;
  return FALLBACK_NAO_TRANSFERIDO.has(extname(caminho).toLowerCase()) ? null : caminho;
}

for (const match of html.matchAll(/(?:src|href)="([^"]+)"/g)) {
  const caminho = resolverLocal(match[1]);
  if (caminho) arquivos.add(caminho);
}

for (const caminho of [...arquivos]) {
  if (extname(caminho).toLowerCase() !== '.css') continue;
  const css = readFileSync(caminho, 'utf8');
  for (const match of css.matchAll(/url\(\s*(['"]?)([^'")]+)\1\s*\)/g)) {
    const fonte = resolverLocal(match[2]);
    if (fonte) arquivos.add(fonte);
  }
}

function categoria(caminho) {
  switch (extname(caminho).toLowerCase()) {
    case '.js':
      return 'JS';
    case '.css':
      return 'CSS';
    case '.html':
      return 'HTML';
    case '.woff2':
      return 'Fontes .woff2';
    default:
      return 'Outros recursos';
  }
}

const linhas = [];
const totais = new Map();
let total = 0;

for (const caminho of [...arquivos].sort()) {
  const conteudo = readFileSync(caminho);
  const extensao = extname(caminho).toLowerCase();
  const modo = JA_COMPRIMIDOS.has(extensao) ? 'original' : 'gzip';
  const bytes = modo === 'gzip' ? gzipSync(conteudo, { level: 9 }).length : conteudo.length;
  const tipo = categoria(caminho);
  linhas.push({ tipo, nome: relative(browserDir, caminho), bytes, modo });
  totais.set(tipo, (totais.get(tipo) ?? 0) + bytes);
  total += bytes;
}

console.log('Bundle inicial — recursos efetivamente transferidos');
console.log('Regra: gzip em JS/CSS/HTML; tamanho original em .woff2 e já comprimidos');
console.log('─'.repeat(72));
for (const linha of linhas) {
  console.log(
    `${linha.tipo.padEnd(16)} ${linha.nome.padEnd(42)} ${String(linha.bytes).padStart(8)} B  ${linha.modo}`,
  );
}
console.log('─'.repeat(72));
for (const [tipo, bytes] of [...totais].sort()) {
  console.log(`${tipo.padEnd(18)} ${String(bytes).padStart(10)} B`);
}
console.log('─'.repeat(72));
console.log(`TOTAL:  ${total} B (${(total / 1024).toFixed(2)} KiB)`);
console.log(`LIMITE: ${LIMITE_GZIP} B (300 KB)`);

if (total > LIMITE_GZIP) {
  console.error(`FALHA: bundle inicial excede o limite em ${total - LIMITE_GZIP} B.`);
  process.exit(1);
}

console.log('OK: bundle inicial dentro do limite.');
