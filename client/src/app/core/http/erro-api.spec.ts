import { describe, expect, it } from 'vitest';

import { ErroApi, mensagemCompreensivel } from './erro-api';

describe('mensagemCompreensivel', () => {
  it('should translate a stable code and include the trace reference when present', () => {
    const mensagem = mensagemCompreensivel(
      { code: 'schedule.slot_unavailable', detail: 'Horário indisponível', traceId: 'trace-123' },
      409,
    );

    expect(mensagem).toContain('horário já está ocupado');
    expect(mensagem).toContain('referência: trace-123');
    expect(mensagem).not.toContain('Horário indisponível');
  });

  it('should describe field errors and keep stable code fallback', () => {
    const mensagem = mensagemCompreensivel(
      {
        code: 'validation.failed',
        errors: { nome: ['Campo obrigatório'], email: ['Formato inválido'] },
      },
      422,
    );

    expect(mensagem).toContain('Revise os campos');
    expect(mensagem).toContain('nome: Campo obrigatório');
    expect(mensagem).toContain('email: Formato inválido');
  });

  it('should not expose internal detail on internal.error', () => {
    const mensagem = mensagemCompreensivel(
      { code: 'internal.error', detail: 'detalhe sensível interno' },
      500,
    );

    expect(mensagem).toContain('erro inesperado');
    expect(mensagem).not.toContain('detalhe sensível interno');
  });

  it('should fall back to a readable message when there is no response body', () => {
    expect(mensagemCompreensivel(undefined, 0)).toBe('Não foi possível conectar ao servidor.');
  });
});

describe('ErroApi', () => {
  it('should carry status, code, traceId and field errors', () => {
    const erro = new ErroApi(409, 'schedule.slot_unavailable', 'trace-123', {}, 'Mensagem');

    expect(erro.status).toBe(409);
    expect(erro.codigo).toBe('schedule.slot_unavailable');
    expect(erro.traceId).toBe('trace-123');
    expect(erro.message).toBe('Mensagem');
    expect(erro.name).toBe('ErroApi');
  });
});
