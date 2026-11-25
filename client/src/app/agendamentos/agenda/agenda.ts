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
import { Table } from '../../shared/ui/table/table';
import { AgendamentosService } from '../agendamentos.service';

type Visao = 'dia' | 'semana' | 'mes';

interface EventoAgenda {
  id: string;
  iniciaEmUtc: string;
  terminaEmUtc: string;
  titulo: string;
  detalhe: string;
  tipo: 'agendamento' | 'bloqueio';
  situacao: string;
}

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, EmptyState, FormField, Table],
  selector: 'app-agenda',
  templateUrl: './agenda.html',
})
export class Agenda implements OnInit {
  private readonly servico = inject(AgendamentosService);
  private readonly barbearias = inject(BarbeariaService);
  private readonly auth = inject(AuthService);

  readonly agenda = this.servico.agenda;
  readonly bloqueios = this.servico.bloqueios;
  readonly profissionais = this.barbearias.profissionais;
  readonly carregando = this.servico.carregando;
  readonly erro = this.servico.erro;
  readonly fuso = computed(() => this.barbearias.barbearia()?.fusoHorarioId);

  readonly eProprietario = computed(
    () =>
      this.auth.conta()?.tipo === 'Professional' &&
      this.barbearias.barbearia()?.codigoConvite != null,
  );
  readonly profissionalId = computed(() =>
    this.eProprietario()
      ? this.selecionadoId()
      : (this.barbearias.vinculo()?.profissionalId ?? null),
  );

  readonly detalheSolicitado = output<void>();

  readonly visao = signal<Visao>('dia');
  readonly data = signal(this.hoje());
  readonly selecionadoId = signal<string | null>(null);

  readonly periodo = computed(() => {
    const data = new Date(`${this.data()}T12:00:00`);
    if (this.visao() === 'dia') return { de: this.data(), ate: this.data() };
    if (this.visao() === 'semana') {
      const inicio = new Date(data);
      inicio.setDate(inicio.getDate() - ((inicio.getDay() + 6) % 7));
      const fim = new Date(inicio);
      fim.setDate(fim.getDate() + 6);
      return { de: this.paraData(inicio), ate: this.paraData(fim) };
    }
    return {
      de: `${data.getFullYear()}-${String(data.getMonth() + 1).padStart(2, '0')}-01`,
      ate: `${data.getFullYear()}-${String(data.getMonth() + 2).padStart(2, '0')}-01`,
    };
  });

  readonly eventos = computed<EventoAgenda[]>(() => {
    const agendamentos = this.agenda().map((item) => ({
      id: item.id,
      iniciaEmUtc: item.iniciaEmUtc,
      terminaEmUtc: item.terminaEmUtc,
      titulo: item.nomeServico,
      detalhe: `${this.formatarHora(item.iniciaEmUtc)} · ${this.formatarHora(item.terminaEmUtc)} · R$ ${item.preco.toFixed(2)}`,
      tipo: 'agendamento' as const,
      situacao: item.situacao,
    }));

    const bloqueios = this.bloqueios().map((item) => ({
      id: item.id,
      iniciaEmUtc: item.iniciaEmUtc,
      terminaEmUtc: item.terminaEmUtc,
      titulo: 'Bloqueio',
      detalhe: `${this.formatarHora(item.iniciaEmUtc)} · ${this.formatarHora(item.terminaEmUtc)} · ${item.motivo ?? 'Sem motivo'}`,
      tipo: 'bloqueio' as const,
      situacao: 'Bloqueado',
    }));

    return [...agendamentos, ...bloqueios].sort((a, b) =>
      a.iniciaEmUtc.localeCompare(b.iniciaEmUtc),
    );
  });

  ngOnInit(): void {
    const barbeariaId = this.barbearias.barbeariaId();
    if (!barbeariaId) return;

    if (!this.barbearias.barbearia()) this.barbearias.obter(barbeariaId);
    this.barbearias.listarProfissionais(barbeariaId);
    this.selecionadoId.set(this.profissionais()[0]?.id ?? null);
    this.carregar();
  }

  abrirDetalhe(id: string): void {
    this.servico.selecionarDetalhe(id);
    this.detalheSolicitado.emit();
  }

  alternar(visao: Visao): void {
    this.visao.set(visao);
    this.carregar();
  }

  selecionar(id: string): void {
    this.selecionadoId.set(id);
    this.carregar();
  }

  navegar(delta: number): void {
    const data = new Date(`${this.data()}T12:00:00`);
    if (this.visao() === 'dia') data.setDate(data.getDate() + delta);
    else if (this.visao() === 'semana') data.setDate(data.getDate() + delta * 7);
    else data.setMonth(data.getMonth() + delta);
    this.data.set(this.paraData(data));
    this.carregar();
  }

  formatarHora(instant: string): string {
    return new Intl.DateTimeFormat('pt-BR', {
      day: '2-digit',
      month: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      timeZone: this.fuso() || undefined,
    }).format(new Date(instant));
  }

  private carregar(): void {
    const barbeariaId = this.barbearias.barbeariaId();
    if (!barbeariaId) return;

    const { de, ate } = this.periodo();
    this.servico.listarAgenda(barbeariaId, de, ate, this.profissionalId());
    const profissional = this.profissionalId();
    if (profissional) this.servico.listarBloqueios(barbeariaId, profissional);
  }

  private hoje(): string {
    return this.paraData(new Date());
  }

  private paraData(data: Date): string {
    return `${data.getFullYear()}-${String(data.getMonth() + 1).padStart(2, '0')}-${String(data.getDate()).padStart(2, '0')}`;
  }
}
