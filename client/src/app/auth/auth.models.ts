/** Contratos da API de autenticação (TDD §11). */
export interface ContaUsuario {
  id: string;
  nomeCompleto: string;
  email: string;
  telefone: string | null;
  tipo: string;
}

/** Cliente não possui senha: autentica por código OTP enviado por e-mail. */
export interface RegistroCliente {
  nomeCompleto: string;
  email: string;
  telefone: string;
}

/** Barbeiro e proprietário mantêm cadastro com e-mail e senha. */
export interface RegistroProfissional {
  nomeCompleto: string;
  email: string;
  telefone: string;
  senha: string;
  confirmarSenha: string;
}

export interface AtualizacaoPerfilCliente {
  nomeCompleto: string;
  email: string;
  telefone: string | null;
}

/** Login por senha, exclusivo de profissionais. */
export interface Login {
  email: string;
  senha: string;
}

export interface SolicitarOtp {
  email: string;
}

export interface VerificarOtp {
  email: string;
  codigo: string;
}

export interface AlteracaoSenha {
  senhaAtual: string;
  novaSenha: string;
  confirmarNovaSenha: string;
}
