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
import { BarbeariaService } from './barbearia/barbearia.service';
import { CadastroBarbearia } from './barbearia/cadastro-barbearia/cadastro-barbearia';
import { EntrarBarbearia } from './barbearia/entrar-barbearia/entrar-barbearia';
import { Profissionais } from './barbearia/profissionais/profissionais';
import { CatalogoServicos } from './servicos/catalogo/catalogo';
import { Agenda } from './agendamentos/agenda/agenda';
import { AgendamentoPublico } from './agendamentos/publico/publico';
import { DetalheAgendamento } from './agendamentos/detalhe/detalhe';
import { MeusAgendamentos } from './agendamentos/meus/meus';
import { Clientes } from './clientes/clientes';
import { Dashboard } from './dashboard/dashboard';
import { Notificacoes } from './notificacoes/notificacoes';
import { NotificacoesService } from './notificacoes/notificacoes.service';
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
  | 'agenda'
  | 'agendar'
  | 'meus'
  | 'detalhe'
  | 'clientes'
  | 'dashboard'
  | 'notificacoes'
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
    Agenda,
    AgendamentoPublico,
    DetalheAgendamento,
    MeusAgendamentos,
    Clientes,
    Dashboard,
    Notificacoes,
    Horarios,
    Disponibilidade,
    Bloqueios,
  ],
  selector: 'app-root',
  templateUrl: './app.html',
})
export class App {
  private readonly auth = inject(AuthService);
  private readonly barbearias = inject(BarbeariaService);
  private readonly notificacoes = inject(NotificacoesService);

  readonly naoLidas = this.notificacoes.naoLidas;

  private readonly telaState = signal<Tela>('escolha');
  private readonly origemDetalhe = signal<Tela>('agenda');
  private readonly perfilState = signal<TipoPerfil>('cliente');

  readonly tela = this.telaState.asReadonly();
  readonly perfil = this.perfilState.asReadonly();
  readonly conta = this.auth.conta;
  readonly erro = this.auth.erro;

  readonly exibirConta = computed(() => this.conta() !== null);
  readonly eProfissional = computed(() => this.conta()?.tipo === 'Professional');
  readonly eProprietario = computed(
    () => this.eProfissional() && this.barbearias.barbearia()?.codigoConvite != null,
  );

  constructor() {
    this.auth.carregarConta();
    effect(() => {
      if (!this.exibirConta()) {
        this.notificacoes.pararPolling();
        return;
      }
      this.notificacoes.iniciarPolling();
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

  abrirAgenda(): void {
    this.auth.limparErro();
    this.telaState.set('agenda');
  }

  abrirAgendar(): void {
    this.auth.limparErro();
    this.telaState.set('agendar');
  }

  abrirNotificacoes(): void {
    this.auth.limparErro();
    this.telaState.set('notificacoes');
  }

  abrirDashboard(): void {
    this.auth.limparErro();
    this.telaState.set('dashboard');
  }

  abrirClientes(): void {
    this.auth.limparErro();
    this.telaState.set('clientes');
  }

  abrirMeus(): void {
    this.auth.limparErro();
    this.telaState.set('meus');
  }

  abrirDetalheDaAgenda(): void {
    this.origemDetalhe.set('agenda');
    this.telaState.set('detalhe');
  }

  abrirDetalheDosMeus(): void {
    this.origemDetalhe.set('meus');
    this.telaState.set('detalhe');
  }

  voltarDeDetalhe(): void {
    this.telaState.set(this.origemDetalhe());
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
