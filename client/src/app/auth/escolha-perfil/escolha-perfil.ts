import { ChangeDetectionStrategy, Component, output } from '@angular/core';

import { Button } from '../../shared/ui/button/button';

export type TipoPerfil = 'cliente' | 'profissional';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button],
  selector: 'app-escolha-perfil',
  templateUrl: './escolha-perfil.html',
})
export class EscolhaPerfil {
  readonly selecionado = output<TipoPerfil>();
  readonly entrar = output<void>();
  readonly entrarProfissional = output<void>();
}
