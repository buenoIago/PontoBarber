import {
  booleanAttribute,
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
} from '@angular/core';

export type Variante = 'primary' | 'secondary' | 'outline';
export type Tamanho = 'sm' | 'lg';
export type Tipo = 'button' | 'submit' | 'reset';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-button',
  templateUrl: './button.html',
})
export class Button {
  readonly variante = input<Variante>('primary');
  readonly tamanho = input<Tamanho | undefined>(undefined);
  readonly tipo = input<Tipo>('button');
  readonly desabilitado = input(false, { transform: booleanAttribute });
  readonly rotuloAria = input<string | undefined>(undefined);

  readonly classes = computed(() =>
    ['btn', `btn-${this.variante()}`, this.tamanho() ? `btn-${this.tamanho()}` : '']
      .filter(Boolean)
      .join(' '),
  );
}
