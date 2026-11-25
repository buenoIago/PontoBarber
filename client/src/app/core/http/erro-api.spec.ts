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
    expect(mensagemCompreensivel(undefined, 401)).toBe('É necessário entrar para continuar.');
    expect(mensagemCompreensivel(undefined, 403)).toBe(
      'Você não tem permissão para esta operação.',
    );
    expect(mensagemCompreensivel(undefined, 404)).toBe('O recurso solicitado não foi encontrado.');
    expect(mensagemCompreensivel(undefined, 409)).toBe('Houve um conflito com o estado atual.');
    expect(mensagemCompreensivel(undefined, 422)).toBe('Revise os campos informados.');
    expect(mensagemCompreensivel(undefined, 500)).toBe('Não foi possível concluir a operação.');
  });

  it('should use detail and title when no stable code is present', () => {
    expect(mensagemCompreensivel({ detail: 'Detalhe da API' }, 400)).toBe('Detalhe da API');
    expect(mensagemCompreensivel({ title: 'Título da API' }, 400)).toBe('Título da API');
  });

  it('should ignore empty field error lists', () => {
    expect(mensagemCompreensivel({ errors: { nome: [], email: ['Inválido'] } }, 400)).toContain(
      'email: Inválido',
    );
    expect(mensagemCompreensivel({ errors: { nome: [] } }, 400)).not.toContain('Verifique');
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
