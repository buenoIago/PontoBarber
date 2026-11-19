import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { finalize } from 'rxjs';

import { ErroApi } from '../core/http/erro-api';
import {
  AlteracaoSenha,
  ContaUsuario,
  Login,
  RegistroCliente,
  RegistroProfissional,
} from './auth.models';

/** Estado da sessão e acesso aos endpoints de autenticação. Sessão por cookie. */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  private readonly contaState = signal<ContaUsuario | null>(null);
  private readonly carregandoState = signal(false);
  private readonly erroState = signal<string | null>(null);

  readonly conta = this.contaState.asReadonly();
  readonly carregando = this.carregandoState.asReadonly();
  readonly erro = this.erroState.asReadonly();

  carregarConta(): void {
    this.executar(
      () => this.http.get<ContaUsuario>('/api/v1/auth/me', { withCredentials: true }),
      (conta) => this.contaState.set(conta),
    );
  }

  cadastrarCliente(dados: RegistroCliente): void {
    this.executar(
      () => this.http.post('/api/v1/auth/register/customer', dados, { withCredentials: true }),
      () => this.carregarConta(),
    );
  }

  cadastrarProfissional(dados: RegistroProfissional): void {
    this.executar(
      () => this.http.post('/api/v1/auth/register/professional', dados, { withCredentials: true }),
      () => this.carregarConta(),
    );
  }

  entrar(dados: Login): void {
    this.executar(
      () => this.http.post('/api/v1/auth/login', dados, { withCredentials: true }),
      () => this.carregarConta(),
    );
  }

  sair(): void {
    this.executar(
      () => this.http.post('/api/v1/auth/logout', {}, { withCredentials: true }),
      () => this.contaState.set(null),
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
