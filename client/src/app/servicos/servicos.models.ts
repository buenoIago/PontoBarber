/** Contratos HTTP do catálogo de serviços (Task 5.4). */

export interface Servico {
  id: string;
  barbeariaId: string;
  nome: string;
  duracaoMinutos: number;
  preco: number | null;
  ativo: boolean;
  ehModelo: boolean;
}

export interface DadosServico {
  nome?: string;
  duracaoMinutos: number;
  preco: number;
  ativo?: boolean;
}
