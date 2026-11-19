/**
 * Contrato de erro da API (ADR-017 / ADR-035) e tradução para mensagens
 * compreensíveis ao usuário. Os códigos vêm do catálogo de CodigosErro.
 */
export interface ProblemaApi {
  title?: string;
  status?: number;
  detail?: string | null;
  code?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

export class ErroApi extends Error {
  constructor(
    readonly status: number,
    readonly codigo: string,
    readonly traceId: string | undefined,
    readonly errosPorCampo: Record<string, string[]>,
    mensagem: string,
  ) {
    super(mensagem);
    this.name = 'ErroApi';
  }
}

const MENSAGENS: Record<string, string> = {
  'auth.invalid_credentials': 'E-mail ou senha incorretos.',
  'auth.locked_out': 'Conta bloqueada temporariamente. Tente novamente mais tarde.',
  'auth.forbidden': 'Você não tem permissão para esta operação.',
  'tenant.not_found': 'Barbearia não encontrada.',
  'tenant.forbidden': 'Você não possui vínculo com esta barbearia.',
  'tenant.invite_conflict': 'Este convite já foi utilizado.',
  'validation.failed': 'Revise os campos informados.',
  'schedule.slot_unavailable': 'Este horário já está ocupado.',
  'schedule.outside_business_hours': 'O horário está fora do funcionamento da barbearia.',
  'schedule.outside_availability': 'O profissional não está disponível neste horário.',
  'schedule.blocked': 'Este horário está bloqueado.',
  'appointment.invalid_state': 'O agendamento não permite esta operação no estado atual.',
  'appointment.not_started': 'O atendimento ainda não foi iniciado.',
  'appointment.already_started': 'O atendimento já foi iniciado.',
  'payment.already_registered': 'O pagamento deste atendimento já foi registrado.',
  'suggestion.expired': 'A sugestão de horário expirou.',
  'concurrency.conflict':
    'Outra alteração foi feita neste registro. Atualize a tela e tente novamente.',
  'idempotency.missing_key': 'Não foi possível identificar a operação. Tente novamente.',
  'rate_limit.exceeded': 'Muitas tentativas. Aguarde um momento e tente novamente.',
  'internal.error': 'Ocorreu um erro inesperado. Tente novamente mais tarde.',
};

function mensagemPadrao(status: number): string {
  if (status === 0) return 'Não foi possível conectar ao servidor.';
  if (status === 401) return 'É necessário entrar para continuar.';
  if (status === 403) return 'Você não tem permissão para esta operação.';
  if (status === 404) return 'O recurso solicitado não foi encontrado.';
  if (status === 409) return 'Houve um conflito com o estado atual.';
  if (status === 422) return 'Revise os campos informados.';
  return 'Não foi possível concluir a operação.';
}

function descreverCampos(erros: Record<string, string[]>): string {
  const partes = Object.entries(erros)
    .filter(([, mensagens]) => mensagens.length > 0)
    .map(([campo, mensagens]) => `${campo}: ${mensagens.join(', ')}`);

  return partes.length > 0 ? ` Verifique ${partes.join('; ')}.` : '';
}

/** Monta uma mensagem legível a partir do ProblemDetails retornado pela API. */
export function mensagemCompreensivel(problema: ProblemaApi | undefined, status: number): string {
  const codigo = problema?.code;
  const base =
    (codigo && MENSAGENS[codigo]) || problema?.detail || problema?.title || mensagemPadrao(status);
  const campos = descreverCampos(problema?.errors ?? {});
  const referencia = problema?.traceId ? ` (referência: ${problema.traceId})` : '';

  return `${base}${campos}${referencia}`;
}
