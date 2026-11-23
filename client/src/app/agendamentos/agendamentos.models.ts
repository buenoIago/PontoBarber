/** Contratos HTTP do fluxo de agendamento (TDD §10.2). */

export interface Agendamento {
  id: string;
  barbeariaId: string;
  profissionalId: string;
  servicoId: string;
  iniciaEmUtc: string;
  terminaEmUtc: string;
  nomeServico: string;
  duracaoMinutos: number;
  preco: number;
  situacao: string;
  origem: string;
  canceladoEmUtc?: string | null;
  canceladoPorUsuarioId?: string | null;
  motivoCancelamento?: string | null;
  inicioRealUtc?: string | null;
  fimRealUtc?: string | null;
  situacaoPagamento?: string | null;
  pagamento?: PagamentoResumo | null;
}

export interface PagamentoResumo {
  id: string;
  valor: number;
  metodo: string;
  registradoEmUtc: string;
}

export interface BloqueioAgenda {
  id: string;
  profissionalId: string;
  iniciaEmUtc: string;
  terminaEmUtc: string;
  motivo: string | null;
}

export interface ServicoDisponivel {
  id: string;
  nome: string;
  duracaoMinutos: number;
  preco: number | null;
  ativo: boolean;
}

export interface Slot {
  profissionalId: string;
  startsAtUtc: string;
  endsAtUtc: string;
  startsAtLocal: string;
}

export interface Disponibilidade {
  date: string;
  fusoHorarioId: string;
  slots: Slot[];
  hasAvailability: boolean;
}

export interface NovoAgendamento {
  profissionalId: string;
  servicoId: string;
  iniciaEmUtc: string;
  observacoes?: string | null;
  clienteId?: string | null;
}

export interface Sugestao {
  id: string;
  agendamentoId: string;
  profissionalId: string;
  inicioSugeridoUtc: string;
  fimSugeridoUtc: string;
  motivo: string;
  situacao: 'Pendente' | 'Aceita' | 'Rejeitada' | 'Expirada';
  expiraEmUtc: string;
  respondidoEmUtc?: string | null;
  autorUsuarioId: string;
}

export interface EventoHistorico {
  id: string;
  agendamentoId: string;
  tipoEvento: string;
  atorUsuarioId: string | null;
  ocorridoEmUtc: string;
  inicioAnteriorUtc: string | null;
  novoInicioUtc: string | null;
  detalhes: string | null;
}
