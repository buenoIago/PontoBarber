import { ChangeDetectionStrategy, Component, inject, output, signal } from '@angular/core';

import { Button } from '../../shared/ui/button/button';
import { FormField } from '../../shared/ui/form-field/form-field';
import { BarbeariaService } from '../barbearia.service';
import type { Vinculo } from '../barbearia.models';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, FormField],
  selector: 'app-entrar-barbearia',
  templateUrl: './entrar-barbearia.html',
})
export class EntrarBarbearia {
  private readonly servico = inject(BarbeariaService);

  readonly entrou = output<Vinculo>();

  readonly vinculo = this.servico.vinculo;
  readonly erro = this.servico.erro;
  readonly carregando = this.servico.carregando;

  readonly codigoConvite = signal('');

  entrar(): void {
    this.servico.entrar(this.codigoConvite());
  }
}
