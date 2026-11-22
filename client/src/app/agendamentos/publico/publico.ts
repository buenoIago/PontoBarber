import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';

import { BarbeariaService } from '../../barbearia/barbearia.service';
import { Button } from '../../shared/ui/button/button';
import { EmptyState } from '../../shared/ui/empty-state/empty-state';
import { FormField } from '../../shared/ui/form-field/form-field';
import { AgendamentosService } from '../agendamentos.service';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, EmptyState, FormField],
  selector: 'app-agendamento-publico',
  templateUrl: './publico.html',
})
export class AgendamentoPublico implements OnInit {
  private readonly servico = inject(AgendamentosService);
  private readonly barbearias = inject(BarbeariaService);

  readonly barbeariaId = this.barbearias.barbeariaId;
  readonly servicos = this.servico.servicos;
  readonly profissionais = this.barbearias.profissionais;
  readonly slots = this.servico.slots;
  readonly carregando = this.servico.carregando;
  readonly erro = this.servico.erro;
  readonly sucesso = this.servico.sucesso;
  readonly fuso = computed(() => this.barbearias.barbearia()?.fusoHorarioId ?? 'Fuso da barbearia');

  readonly servicoId = signal('');
  readonly profissionalId = signal('');
  readonly data = signal('');
  readonly horario = signal('');
  readonly observacoes = signal('');
  readonly idBarbeariaInformado = signal('');

  selecionarBarbearia(): void {
    const id = this.idBarbeariaInformado().trim();
    if (!id) return;

    this.barbearias.selecionar(id);
    this.barbearias.obter(id);
    this.carregarDados();
  }

  ngOnInit(): void {
    this.servico.limparMensagens();
    this.carregarDados();
  }

  private carregarDados(): void {
    const barbeariaId = this.barbearias.barbeariaId();
    if (!barbeariaId) return;

    this.servico.listarServicos(barbeariaId);
    this.barbearias.listarProfissionais(barbeariaId);
  }

  selecionarData(valor: string): void {
    this.data.set(valor);
    this.horario.set('');
    this.consultar();
  }

  consultar(): void {
    const barbeariaId = this.barbearias.barbeariaId();
    if (!barbeariaId || !this.servicoId() || !this.profissionalId() || !this.data()) return;

    this.servico.consultarSlots(
      barbeariaId, this.servicoId(), this.profissionalId(), this.data(),
    );
  }

  confirmar(): void {
    const barbeariaId = this.barbearias.barbeariaId();
    if (!barbeariaId || !this.horario()) return;

    this.servico.confirmar(barbeariaId, {
      profissionalId: this.profissionalId(),
      servicoId: this.servicoId(),
      iniciaEmUtc: this.horario(),
      observacoes: this.observacoes().trim() || null,
    }, true, () => this.horario.set(''));
  }

  formatar(instant: string): string {
    return new Intl.DateTimeFormat('pt-BR', {
      day: '2-digit',
      month: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      timeZone: this.barbearias.barbearia()?.fusoHorarioId || undefined,
    }).format(new Date(instant));
  }
}
