import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnInit,
  signal,
} from '@angular/core';

import { AuthService } from '../../auth/auth.service';
import { BarbeariaService } from '../../barbearia/barbearia.service';
import { Button } from '../../shared/ui/button/button';
import { EmptyState } from '../../shared/ui/empty-state/empty-state';
import { Table } from '../../shared/ui/table/table';
import { DIAS_SEMANA, type DiaFuncionamento } from '../configuracoes.models';
import { ConfiguracoesService } from '../configuracoes.service';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, EmptyState, Table],
  selector: 'app-horarios-config',
  templateUrl: './horarios.html',
})
export class Horarios implements OnInit {
  private readonly servico = inject(ConfiguracoesService);
  private readonly barbearias = inject(BarbeariaService);
  private readonly auth = inject(AuthService);

  readonly dias = DIAS_SEMANA;
  readonly horarios = this.servico.horarios;
  readonly carregando = this.servico.carregando;
  readonly erro = this.servico.erro;
  readonly sucesso = this.servico.sucesso;

  readonly eProprietario = computed(
    () =>
      this.auth.conta()?.tipo === 'Professional' &&
      this.barbearias.barbearia()?.codigoConvite != null,
  );

  readonly editando = signal(false);
  readonly rascunho = signal<DiaFuncionamento[]>([]);

  ngOnInit(): void {
    this.servico.limparMensagens();
    const barbeariaId = this.barbearias.barbeariaId();
    if (!barbeariaId) return;

    if (!this.barbearias.barbearia()) this.barbearias.obter(barbeariaId);
    this.servico.listarHorarios(barbeariaId);
  }

  iniciarEdicao(): void {
    this.servico.limparMensagens();
    this.rascunho.set(this.horarios().map((dia) => ({ ...dia })));
    this.editando.set(true);
  }

  cancelar(): void {
    this.editando.set(false);
    this.rascunho.set([]);
  }

  salvar(): void {
    const barbeariaId = this.barbearias.barbeariaId();
    if (!barbeariaId) return;

    const dias = this.rascunho().map((dia) => ({
      diaSemana: dia.diaSemana,
      abreAs: this.paraApi(dia.abreAs),
      fechaAs: this.paraApi(dia.fechaAs),
      fechado: dia.fechado,
    }));

    this.servico.salvarHorarios(barbeariaId, dias, () => this.cancelar());
  }

  alternarFechado(diaSemana: number, fechado: boolean): void {
    this.rascunho.update((dias) =>
      dias.map((dia) => (dia.diaSemana === diaSemana ? { ...dia, fechado } : dia)),
    );
  }

  definirHorario(diaSemana: number, campo: 'abreAs' | 'fechaAs', valor: string): void {
    this.rascunho.update((dias) =>
      dias.map((dia) => (dia.diaSemana === diaSemana ? { ...dia, [campo]: valor || null } : dia)),
    );
  }

  horaTela(valor: string | null): string {
    return valor ? valor.slice(0, 5) : '';
  }

  private paraApi(valor: string | null): string | null {
    return valor ? `${valor.slice(0, 5)}:00` : null;
  }
}
