/** Contratos da consulta administrativa de clientes (TDD §10.2). */

export interface Cliente {
  id: string;
  nomeCompleto: string;
  email: string;
  telefone: string | null;
}

export interface ListaClientes {
  itens: Cliente[];
  total: number;
  pagina: number;
  tamanho: number;
}
