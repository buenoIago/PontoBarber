/** Contrato do dashboard do proprietário (TDD §10.2). */

export interface ProximoAgendamento {
  id: string;
  iniciaEmUtc: string;
  nomeCliente: string;
  nomeProfissional: string;
  situacao: string;
}

export interface DashboardResumo {
  hoje: number;
  estaSemana: number;
  cancelamentos: number;
  recebidoNoMes: number;
  proximos: ProximoAgendamento[];
}
