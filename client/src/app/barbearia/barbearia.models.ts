/** Contratos HTTP do módulo de barbearia (TDD §10.2). */

export interface Barbearia {
  id: string;
  nome: string;
  telefoneComercial: string;
  emailComercial: string;
  cidade: string;
  endereco: string;
  instagram: string | null;
  codigoConvite: string | null;
  fusoHorarioId: string;
}

export interface CriarBarbearia {
  nome: string;
  telefoneComercial: string;
  emailComercial: string;
  cidade: string;
  endereco: string;
  instagram: string | null;
  fusoHorarioId: string;
}

export interface Profissional {
  id: string;
  nomeCompleto: string;
  telefone: string | null;
  situacao: string;
}

export interface Vinculo {
  id: string;
  barbeariaId: string;
  profissionalId: string;
  cargo: string;
  situacao: string;
}
