import {
  booleanAttribute,
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
} from '@angular/core';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-form-field',
  templateUrl: './form-field.html',
})
export class FormField {
  readonly id = input.required<string>();
  readonly rotulo = input.required<string>();
  readonly dica = input<string | undefined>(undefined);
  readonly erro = input<string | undefined>(undefined);
  readonly obrigatorio = input(false, { transform: booleanAttribute });

  readonly idDica = computed(() => `${this.id()}-dica`);
  readonly idErro = computed(() => `${this.id()}-erro`);
}
