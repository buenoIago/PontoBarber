/** Contratos das notificações internas (TDD §10.2). */

export interface Notificacao {
  id: string;
  barbeariaId: string;
  tipo: string;
  agendamentoId: string | null;
  mensagem: string;
  criadoEmUtc: string;
  lidoEmUtc: string | null;
}

export interface ListaNotificacoes {
  itens: Notificacao[];
  naoLidas: number;
  pagina: number;
  tamanho: number;
}
