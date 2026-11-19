import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';

import { AuthService } from './auth/auth.service';
import { Cadastro } from './auth/cadastro/cadastro';
import { Conta } from './auth/conta/conta';
import { EscolhaPerfil, type TipoPerfil } from './auth/escolha-perfil/escolha-perfil';
import { Login } from './auth/login/login';

type Tela = 'escolha' | 'cadastro' | 'login' | 'conta';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Cadastro, Conta, EscolhaPerfil, Login],
  selector: 'app-root',
  templateUrl: './app.html',
})
export class App {
  private readonly auth = inject(AuthService);

  private readonly telaState = signal<Tela>('escolha');
  private readonly perfilState = signal<TipoPerfil>('cliente');

  readonly tela = this.telaState.asReadonly();
  readonly perfil = this.perfilState.asReadonly();
  readonly conta = this.auth.conta;
  readonly erro = this.auth.erro;

  readonly exibirConta = computed(() => this.conta() !== null);

  constructor() {
    this.auth.carregarConta();
  }

  escolherPerfil(perfil: TipoPerfil): void {
    this.perfilState.set(perfil);
    this.auth.limparErro();
    this.telaState.set('cadastro');
  }

  abrirCadastro(): void {
    this.auth.limparErro();
    this.telaState.set('cadastro');
  }

  abrirLogin(): void {
    this.auth.limparErro();
    this.telaState.set('login');
  }

  abrirEscolha(): void {
    this.auth.limparErro();
    this.telaState.set('escolha');
  }
}
