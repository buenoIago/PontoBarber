import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  OnInit,
  signal,
} from '@angular/core';

import { AuthService } from '../../auth/auth.service';
import { BarbeariaService } from '../../barbearia/barbearia.service';
import { Button } from '../../shared/ui/button/button';
import { EmptyState } from '../../shared/ui/empty-state/empty-state';
import { FormField } from '../../shared/ui/form-field/form-field';
import { Table } from '../../shared/ui/table/table';
import { ConfiguracoesService } from '../configuracoes.service';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, EmptyState, FormField, Table],
  selector: 'app-bloqueios-config',
  templateUrl: './bloqueios.html',
})
export class Bloqueios implements OnInit {
  private readonly servico = inject(ConfiguracoesService);
  private readonly barbearias = inject(BarbeariaService);
  private readonly auth = inject(AuthService);

  readonly profissionais = this.barbearias.profissionais;
  readonly bloqueios = this.servico.bloqueios;
  readonly carregando = this.servico.carregando;
  readonly erro = this.servico.erro;
  readonly sucesso = this.servico.sucesso;

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
  readonly podeEditar = computed(() => this.profissionalId() !== null);

  readonly selecionadoId = signal<string | null>(null);
  readonly inicio = signal('');
  readonly termino = signal('');
  readonly motivo = signal('');

  constructor() {
    effect(() => {
      const id = this.profissionalId();
      const barbeariaId = this.barbearias.barbeariaId();
      if (!id || !barbeariaId) return;

      this.servico.listarBloqueios(barbeariaId, id);
    });
  }

  ngOnInit(): void {
    this.servico.limparMensagens();
    const barbeariaId = this.barbearias.barbeariaId();
    if (!barbeariaId) return;

    if (!this.barbearias.barbearia()) this.barbearias.obter(barbeariaId);
    this.barbearias.listarProfissionais(barbeariaId);
    this.selecionadoId.set(this.profissionais()[0]?.id ?? null);
  }

  selecionar(id: string): void {
    this.servico.limparMensagens();
    this.selecionadoId.set(id);
  }

  criar(): void {
    const barbeariaId = this.barbeariaId();
    const profissionalId = this.profissionalId();
    if (!barbeariaId || !profissionalId || !this.inicio() || !this.termino()) return;

    this.servico.criarBloqueio(
      barbeariaId,
      profissionalId,
      {
        iniciaEmUtc: `${this.inicio()}:00Z`,
        terminaEmUtc: `${this.termino()}:00Z`,
        motivo: this.motivo().trim() || null,
      },
      () => {
        this.inicio.set('');
        this.termino.set('');
        this.motivo.set('');
      },
    );
  }

  remover(bloqueioId: string): void {
    const barbeariaId = this.barbeariaId();
    const profissionalId = this.profissionalId();
    if (!barbeariaId || !profissionalId) return;

    this.servico.removerBloqueio(barbeariaId, profissionalId, bloqueioId);
  }

  formatar(instant: string): string {
    const fuso = this.barbearias.barbearia()?.fusoHorarioId;
    return new Intl.DateTimeFormat('pt-BR', {
      dateStyle: 'short',
      timeStyle: 'short',
      timeZone: fuso || undefined,
    }).format(new Date(instant));
  }

  private barbeariaId(): string | null {
    return this.barbearias.barbeariaId();
  }
}
