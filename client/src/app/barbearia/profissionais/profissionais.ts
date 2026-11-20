import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';

import { Button } from '../../shared/ui/button/button';
import { EmptyState } from '../../shared/ui/empty-state/empty-state';
import { FormField } from '../../shared/ui/form-field/form-field';
import { Table } from '../../shared/ui/table/table';
import { type Profissional } from '../barbearia.models';
import { BarbeariaService } from '../barbearia.service';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, EmptyState, FormField, Table],
  selector: 'app-profissionais',
  templateUrl: './profissionais.html',
})
export class Profissionais {
  private readonly servico = inject(BarbeariaService);

  readonly barbeariaId = this.servico.barbeariaId;
  readonly profissionais = this.servico.profissionais;
  readonly erro = this.servico.erro;
  readonly carregando = this.servico.carregando;

  readonly editandoId = signal<string | null>(null);
  readonly nomeEditado = signal('');
  readonly telefoneEditado = signal('');

  constructor() {
    effect(() => {
      const barbeariaId = this.barbeariaId();
      if (barbeariaId) this.servico.listarProfissionais(barbeariaId);
    });
  }

  recarregar(): void {
    const barbeariaId = this.barbeariaId();
    if (barbeariaId) this.servico.listarProfissionais(barbeariaId);
  }

  iniciarEdicao(profissional: Profissional): void {
    this.editandoId.set(profissional.id);
    this.nomeEditado.set(profissional.nomeCompleto);
    this.telefoneEditado.set(profissional.telefone ?? '');
  }

  cancelarEdicao(): void {
    this.editandoId.set(null);
  }

  salvarEdicao(profissionalId: string): void {
    const barbeariaId = this.barbeariaId();
    if (!barbeariaId) return;

    const telefone = this.telefoneEditado().trim();
    this.servico.atualizarDados(barbeariaId, profissionalId, {
      nomeCompleto: this.nomeEditado(),
      telefone: telefone || null,
    });
    this.editandoId.set(null);
  }

  alternarStatus(profissional: Profissional): void {
    const barbeariaId = this.barbeariaId();
    if (!barbeariaId) return;

    this.servico.alterarStatus(barbeariaId, profissional.id, profissional.situacao !== 'Ativo');
  }
}
