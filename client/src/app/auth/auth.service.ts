import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { finalize } from 'rxjs';

import { BarbeariaService } from '../barbearia/barbearia.service';
import { ErroApi } from '../core/http/erro-api';
import {
  AlteracaoSenha,
  AtualizacaoPerfilCliente,
  ContaUsuario,
  Login,
  RegistroCliente,
  RegistroProfissional,
  VerificarOtp,
} from './auth.models';

/** Estado da sessão e acesso aos endpoints de autenticação. Sessão por cookie. */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly barbearias = inject(BarbeariaService);

  private readonly contaState = signal<ContaUsuario | null>(null);
  private readonly carregandoState = signal(false);
  private readonly erroState = signal<string | null>(null);
  private readonly sucessoState = signal<string | null>(null);
  private readonly emailOtpState = signal<string | null>(null);

  readonly conta = this.contaState.asReadonly();
  readonly carregando = this.carregandoState.asReadonly();
  readonly erro = this.erroState.asReadonly();
  readonly sucesso = this.sucessoState.asReadonly();
  /** E-mail que recebeu o código; não nulo enquanto a etapa OTP está aberta. */
  readonly emailOtp = this.emailOtpState.asReadonly();

  carregarConta(): void {
    this.executar(
      () => this.http.get<ContaUsuario>('/api/v1/auth/me', { withCredentials: true }),
      (conta) => {
        if (!conta) return;
        this.contaState.set(conta);
        this.barbearias.restaurarVinculos(conta.vinculos ?? []);
      },
    );
  }

  cadastrarCliente(dados: RegistroCliente): void {
    this.executar(
      () => this.http.post('/api/v1/auth/cadastro/cliente', dados, { withCredentials: true }),
      () => this.sucessoState.set('Cadastro realizado. Solicite o código de acesso por e-mail.'),
    );
  }

  cadastrarProfissional(dados: RegistroProfissional): void {
    this.executar(
      () => this.http.post('/api/v1/auth/cadastro/profissional', dados, { withCredentials: true }),
      () => this.sucessoState.set('Cadastro realizado. Entre com seu e-mail e senha.'),
    );
  }

  entrar(dados: Login): void {
    this.executar(
      () => this.http.post('/api/v1/auth/login', dados, { withCredentials: true }),
      () => this.carregarConta(),
    );
  }

  /** Solicita o OTP. A resposta é genérica, mesmo se o e-mail não existir. */
  solicitarCodigo(email: string): void {
    // Não limpar antes da resposta: um reenvio com falha deve manter a tela
    // de código aberta para o usuário poder tentar novamente.
    this.executar(
      () => this.http.post('/api/v1/auth/otp/solicitar', { email }, { withCredentials: true }),
      () => {
        this.emailOtpState.set(email);
        this.sucessoState.set('Se a conta existir, um código foi enviado por e-mail.');
      },
    );
  }

  /** Valida o código e abre a sessão pelo cookie existente. */
  verificarCodigo(dados: VerificarOtp): void {
    this.executar(
      () => this.http.post('/api/v1/auth/otp/verificar', dados, { withCredentials: true }),
      () => {
        this.emailOtpState.set(null);
        this.carregarConta();
      },
    );
  }

  cancelarOtp(): void {
    this.emailOtpState.set(null);
  }

  sair(): void {
    this.executar(
      () => this.http.post('/api/v1/auth/logout', {}, { withCredentials: true }),
      () => {
        this.contaState.set(null);
        this.emailOtpState.set(null);
        this.barbearias.limpar();
      },
    );
  }

  atualizarPerfilCliente(dados: AtualizacaoPerfilCliente): void {
    this.executar(
      () =>
        this.http.put<{ nomeCompleto: string; email: string; telefone: string | null }>(
          '/api/v1/minha-conta/perfil-cliente',
          dados,
          { withCredentials: true },
        ),
      (conta) => {
        this.contaState.update((atual) =>
          atual
            ? {
                ...atual,
                nomeCompleto: conta.nomeCompleto,
                email: conta.email,
                telefone: conta.telefone,
              }
            : atual,
        );
        this.sucessoState.set('Dados atualizados.');
      },
    );
  }

  alterarSenha(dados: AlteracaoSenha): void {
    this.executar(
      () => this.http.post('/api/v1/auth/change-password', dados, { withCredentials: true }),
      () => this.erroState.set(null),
    );
  }

  limparErro(): void {
    this.erroState.set(null);
  }

  private executar<T>(
    operacao: () => import('rxjs').Observable<T>,
    sucesso: (resposta: T) => void,
  ): void {
    this.erroState.set(null);
    this.sucessoState.set(null);
    this.carregandoState.set(true);

    operacao()
      .pipe(finalize(() => this.carregandoState.set(false)))
      .subscribe({
        next: (resposta) => sucesso(resposta),
        error: (erro: unknown) =>
          this.erroState.set(
            erro instanceof ErroApi ? erro.message : 'Não foi possível concluir a operação.',
          ),
      });
  }
}
