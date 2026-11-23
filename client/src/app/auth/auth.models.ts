/** Contratos da API de autenticação (TDD §11). */
export interface ContaUsuario {
  id: string;
  nomeCompleto: string;
  email: string;
  telefone: string | null;
  tipo: string;
}

export interface RegistroCliente {
  nomeCompleto: string;
  email: string;
  telefone: string;
  senha: string;
  confirmarSenha: string;
}

export type RegistroProfissional = RegistroCliente;

export interface AtualizacaoPerfilCliente {
  nomeCompleto: string;
  email: string;
  telefone: string | null;
}

export interface Login {
  email: string;
  senha: string;
}

export interface AlteracaoSenha {
  senhaAtual: string;
  novaSenha: string;
  confirmarNovaSenha: string;
}
