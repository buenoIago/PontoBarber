import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnInit,
  output,
  signal,
} from '@angular/core';

import { AuthService } from '../../auth/auth.service';
import { BarbeariaService } from '../../barbearia/barbearia.service';
import { Button } from '../../shared/ui/button/button';
import { EmptyState } from '../../shared/ui/empty-state/empty-state';
import { FormField } from '../../shared/ui/form-field/form-field';
import { AgendamentosService } from '../agendamentos.service';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, EmptyState, FormField],
  selector: 'app-detalhe-agendamento',
  templateUrl: './detalhe.html',
})
export class DetalheAgendamento implements OnInit {
  private readonly servico = inject(AgendamentosService);
  private readonly barbearias = inject(BarbeariaService);
  private readonly auth = inject(AuthService);

  readonly voltar = output<void>();

  readonly detalhe = this.servico.detalhe;
  readonly historico = this.servico.historico;
  readonly sugestoes = this.servico.sugestoes;
  readonly slots = this.servico.slots;
  readonly carregando = this.servico.carregando;
  readonly erro = this.servico.erro;
  readonly sucesso = this.servico.sucesso;
  readonly dadosPublicos = this.barbearias.dadosPublicos;
  readonly eCliente = computed(() => this.auth.conta()?.tipo === 'Customer');
  readonly fuso = computed(() =>
    this.eCliente()
      ? this.dadosPublicos()?.fusoHorarioId
      : this.barbearias.barbearia()?.fusoHorarioId,
  );
  // TDD §5.5.3: atendimento e pagamento exigem vínculo ativo e titularidade
  // da agenda. Proprietário e barbeiro seguem a mesma regra; o backend valida
  // perfil.Id == agendamento.ProfissionalId (ServicoAtendimentos).
  readonly eBarbeiro = computed(() => {
    if (this.auth.conta()?.tipo !== 'Professional') return false;
    const vinculo = this.barbearias.vinculo();
    if (!vinculo || vinculo.situacao !== 'Ativo') return false;
    return vinculo.profissionalId === this.detalhe()?.profissionalId;
  });

  readonly dataHorario = signal('');
  readonly horario = signal('');
  readonly motivoCancelamento = signal('');
  readonly motivoSugestao = signal('');
  readonly confirmandoCancelamento = signal(false);
  readonly agora = signal(Date.now());

  readonly valorPagamento = signal(0);
  readonly metodoPagamento = signal('Pix');

  readonly podeIniciar = computed(
    () => this.eBarbeiro() && this.detalhe()?.situacao === 'Confirmado',
  );
  readonly podeConcluir = computed(
    () => this.eBarbeiro() && this.detalhe()?.situacao === 'EmAndamento',
  );
  readonly podeRegistrarAusencia = computed(
    () => this.eBarbeiro() && this.detalhe()?.situacao === 'Confirmado',
  );
  readonly podePagar = computed(
    () =>
      this.eBarbeiro() &&
      this.detalhe()?.situacaoPagamento === 'Pendente' &&
      !['Cancelado', 'NaoCompareceu'].includes(this.detalhe()?.situacao ?? ''),
  );

  readonly pendentes = computed(() =>
    this.sugestoes().filter(
      (sugestao) =>
        sugestao.situacao === 'Pendente' && new Date(sugestao.expiraEmUtc).getTime() > this.agora(),
    ),
  );

  ngOnInit(): void {
    const barbeariaId = this.barbearias.barbeariaId();
    const agendamentoId = this.servico.detalheId();
    this.agora.set(Date.now());
    if (!barbeariaId || !agendamentoId) return;

    if (this.eCliente()) {
      if (this.dadosPublicos()?.barbeariaId !== barbeariaId)
        this.barbearias.obterDadosAgendamentoPublico(barbeariaId);
    } else if (!this.barbearias.barbearia()) {
      this.barbearias.obter(barbeariaId);
    }
    this.servico.limparMensagens();
    this.servico.carregarDetalhe(barbeariaId, agendamentoId);
  }

  selecionarData(valor: string): void {
    this.dataHorario.set(valor);
    this.horario.set('');
    const detalhe = this.detalhe();
    const barbeariaId = this.barbearias.barbeariaId();
    if (!detalhe || !barbeariaId || !valor) return;

    this.servico.consultarSlots(barbeariaId, detalhe.servicoId, detalhe.profissionalId, valor);
  }

  cancelar(): void {
    const barbeariaId = this.barbearias.barbeariaId();
    const id = this.detalhe()?.id;
    if (!barbeariaId || !id) return;

    this.servico.cancelar(barbeariaId, id, this.motivoCancelamento(), () => this.recarregar());
    this.confirmandoCancelamento.set(false);
  }

  remarcar(): void {
    const barbeariaId = this.barbearias.barbeariaId();
    const id = this.detalhe()?.id;
    if (!barbeariaId || !id || !this.horario()) return;

    this.servico.remarcar(barbeariaId, id, this.horario(), () => this.recarregar());
  }

  sugerir(): void {
    const barbeariaId = this.barbearias.barbeariaId();
    const id = this.detalhe()?.id;
    if (!barbeariaId || !id || !this.horario() || !this.motivoSugestao().trim()) return;

    this.servico.sugerir(barbeariaId, id, this.horario(), this.motivoSugestao().trim(), () =>
      this.recarregar(),
    );
  }

  iniciar(): void {
    const barbeariaId = this.barbearias.barbeariaId();
    const id = this.detalhe()?.id;
    if (!barbeariaId || !id) return;

    this.servico.iniciar(barbeariaId, id, () => this.recarregar());
  }

  concluir(): void {
    const barbeariaId = this.barbearias.barbeariaId();
    const id = this.detalhe()?.id;
    if (!barbeariaId || !id) return;

    this.servico.concluir(barbeariaId, id, () => this.recarregar());
  }

  registrarAusencia(): void {
    const barbeariaId = this.barbearias.barbeariaId();
    const id = this.detalhe()?.id;
    if (!barbeariaId || !id) return;

    this.servico.registrarAusencia(barbeariaId, id, () => this.recarregar());
  }

  pagar(): void {
    const barbeariaId = this.barbearias.barbeariaId();
    const id = this.detalhe()?.id;
    if (!barbeariaId || !id || this.valorPagamento() < 0) return;

    this.servico.registrarPagamento(
      barbeariaId,
      id,
      this.valorPagamento(),
      this.metodoPagamento(),
      () => this.recarregar(),
    );
  }

  responder(sugestaoId: string, aceitar: boolean): void {
    const barbeariaId = this.barbearias.barbeariaId();
    const id = this.detalhe()?.id;
    if (!barbeariaId || !id) return;

    this.servico.responderSugestao(barbeariaId, id, sugestaoId, aceitar, () => this.recarregar());
  }

  formatar(instant: string | null | undefined): string {
    if (!instant) return '—';
    return new Intl.DateTimeFormat('pt-BR', {
      day: '2-digit',
      month: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      timeZone: this.fuso() || undefined,
    }).format(new Date(instant));
  }

  rotuloEvento(tipo: string): string {
    const rotulos: Record<string, string> = {
      Criacao: 'Criação',
      Confirmacao: 'Confirmação',
      Cancelamento: 'Cancelamento',
      Remarcacao: 'Remarcação',
      SugestaoCriada: 'Sugestão criada',
      SugestaoAceita: 'Sugestão aceita',
      SugestaoRejeitada: 'Sugestão rejeitada',
      SugestaoExpirada: 'Sugestão expirada',
      NaoComparecimento: 'Não comparecimento',
      ServicoIniciado: 'Atendimento iniciado',
      ServicoConcluido: 'Atendimento concluído',
      PagamentoRegistrado: 'Pagamento registrado',
    };
    return rotulos[tipo] ?? tipo;
  }

  voltarParaOrigem(): void {
    this.voltar.emit();
  }

  private recarregar(): void {
    const barbeariaId = this.barbearias.barbeariaId();
    const id = this.detalhe()?.id;
    this.horario.set('');
    this.motivoSugestao.set('');
    this.agora.set(Date.now());
    if (!barbeariaId || !id) return;

    this.servico.carregarDetalhe(barbeariaId, id);
  }
}
