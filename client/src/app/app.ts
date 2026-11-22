import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';

import { AuthService } from './auth/auth.service';
import { Cadastro } from './auth/cadastro/cadastro';
import { Conta } from './auth/conta/conta';
import { EscolhaPerfil, type TipoPerfil } from './auth/escolha-perfil/escolha-perfil';
import { Login } from './auth/login/login';
import { CadastroBarbearia } from './barbearia/cadastro-barbearia/cadastro-barbearia';
import { EntrarBarbearia } from './barbearia/entrar-barbearia/entrar-barbearia';
import { Profissionais } from './barbearia/profissionais/profissionais';
import { CatalogoServicos } from './servicos/catalogo/catalogo';
import { Bloqueios } from './configuracoes/bloqueios/bloqueios';
import { Disponibilidade } from './configuracoes/disponibilidade/disponibilidade';
import { Horarios } from './configuracoes/horarios/horarios';
import { Button } from './shared/ui/button/button';

type Tela =
  | 'escolha'
  | 'cadastro'
  | 'login'
  | 'conta'
  | 'criar-barbearia'
  | 'entrar-barbearia'
  | 'profissionais'
  | 'servicos'
  | 'horarios'
  | 'disponibilidade'
  | 'bloqueios';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    Button,
    Cadastro,
    Conta,
    EscolhaPerfil,
    Login,
    CadastroBarbearia,
    EntrarBarbearia,
    Profissionais,
    CatalogoServicos,
    Horarios,
    Disponibilidade,
    Bloqueios,
  ],
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
  readonly eProfissional = computed(() => this.conta()?.tipo === 'Professional');

  constructor() {
    this.auth.carregarConta();
    effect(() => {
      if (!this.exibirConta()) return;
      const atual = this.telaState();
      if (atual === 'escolha' || atual === 'cadastro' || atual === 'login')
        this.telaState.set('conta');
    });
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

  abrirConta(): void {
    this.auth.limparErro();
    this.telaState.set('conta');
  }

  abrirCriarBarbearia(): void {
    this.auth.limparErro();
    this.telaState.set('criar-barbearia');
  }

  abrirEntrarBarbearia(): void {
    this.auth.limparErro();
    this.telaState.set('entrar-barbearia');
  }

  abrirProfissionais(): void {
    this.auth.limparErro();
    this.telaState.set('profissionais');
  }

  abrirServicos(): void {
    this.auth.limparErro();
    this.telaState.set('servicos');
  }

  abrirHorarios(): void {
    this.auth.limparErro();
    this.telaState.set('horarios');
  }

  abrirDisponibilidade(): void {
    this.auth.limparErro();
    this.telaState.set('disponibilidade');
  }

  abrirBloqueios(): void {
    this.auth.limparErro();
    this.telaState.set('bloqueios');
  }
}
