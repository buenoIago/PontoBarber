import { ChangeDetectionStrategy, Component, inject, output, signal } from '@angular/core';

import { Button } from '../../shared/ui/button/button';
import { FormField } from '../../shared/ui/form-field/form-field';
import { BarbeariaService } from '../barbearia.service';
import type { Barbearia } from '../barbearia.models';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, FormField],
  selector: 'app-cadastro-barbearia',
  templateUrl: './cadastro-barbearia.html',
})
export class CadastroBarbearia {
  private readonly servico = inject(BarbeariaService);

  readonly criada = output<Barbearia>();

  readonly barbearia = this.servico.barbearia;
  readonly erro = this.servico.erro;
  readonly carregando = this.servico.carregando;

  readonly nome = signal('');
  readonly telefoneComercial = signal('');
  readonly emailComercial = signal('');
  readonly cidade = signal('');
  readonly endereco = signal('');
  readonly instagram = signal('');
  readonly fusoHorarioId = signal('America/Sao_Paulo');

  enviar(): void {
    const instagram = this.instagram().trim();
    this.servico.criar({
      nome: this.nome(),
      telefoneComercial: this.telefoneComercial(),
      emailComercial: this.emailComercial(),
      cidade: this.cidade(),
      endereco: this.endereco(),
      instagram: instagram || null,
      fusoHorarioId: this.fusoHorarioId(),
    });
  }
}
