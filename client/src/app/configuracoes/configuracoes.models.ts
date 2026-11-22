/** Contratos HTTP das telas de configuração (TDD §10.2). */

export interface DiaFuncionamento {
  diaSemana: number;
  abreAs: string | null;
  fechaAs: string | null;
  fechado: boolean;
}

export interface JanelaDisponibilidade {
  id?: string;
  diaSemana: number;
  iniciaEm: string;
  terminaEm: string;
}

export interface Bloqueio {
  id: string;
  profissionalId: string;
  iniciaEmUtc: string;
  terminaEmUtc: string;
  motivo: string | null;
}

export const DIAS_SEMANA: ReadonlyArray<{ numero: number; nome: string }> = [
  { numero: 1, nome: 'Segunda' },
  { numero: 2, nome: 'Terça' },
  { numero: 3, nome: 'Quarta' },
  { numero: 4, nome: 'Quinta' },
  { numero: 5, nome: 'Sexta' },
  { numero: 6, nome: 'Sábado' },
  { numero: 7, nome: 'Domingo' },
];
