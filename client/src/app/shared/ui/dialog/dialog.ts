import {
  ChangeDetectionStrategy,
  Component,
  effect,
  ElementRef,
  input,
  model,
  viewChild,
} from '@angular/core';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-dialog',
  templateUrl: './dialog.html',
})
export class Dialog {
  private static proximoId = 0;

  readonly aberto = model(false);
  readonly titulo = input.required<string>();
  readonly tituloId = `pb-dialog-${Dialog.proximoId++}`;

  private readonly dialogRef = viewChild<ElementRef<HTMLDialogElement>>('dialog');

  constructor() {
    effect(() => {
      const elemento = this.dialogRef()?.nativeElement;
      if (!elemento) return;

      if (this.aberto() && !elemento.open) elemento.showModal();
      if (!this.aberto() && elemento.open) elemento.close();
    });
  }

  fechar(): void {
    this.aberto.set(false);
  }

  aoFechar(): void {
    this.aberto.set(false);
  }

  aoClicar(evento: MouseEvent): void {
    if (evento.target === this.dialogRef()?.nativeElement) this.aberto.set(false);
  }
}
