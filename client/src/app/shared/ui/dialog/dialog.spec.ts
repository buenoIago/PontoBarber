import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { violacoesCriticas } from '../../../../testing/axe';
import { Dialog } from './dialog';

@Component({
  imports: [Dialog],
  template: `
    <app-dialog [(aberto)]="aberto" titulo="Confirmar exclusão">
      <p>Tem certeza?</p>
    </app-dialog>
  `,
})
class HostDialog {
  readonly aberto = signal(false);
}

describe('Dialog', () => {
  let fixture: ComponentFixture<HostDialog>;

  async function criar(): Promise<HTMLElement> {
    await TestBed.configureTestingModule({
      imports: [HostDialog],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();

    fixture = TestBed.createComponent(HostDialog);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  function dialog(elemento: HTMLElement): HTMLDialogElement {
    const encontrado = elemento.querySelector('dialog');
    if (!encontrado) throw new Error('dialog não renderizado');
    return encontrado as HTMLDialogElement;
  }

  it('should open with an accessible name when aberto becomes true', async () => {
    const elemento = await criar();
    expect(dialog(elemento).hasAttribute('open')).toBe(false);

    fixture.componentInstance.aberto.set(true);
    await fixture.whenStable();

    const elementoDialogo = dialog(elemento);
    const titulo = elemento.querySelector('h2');
    expect(elementoDialogo.hasAttribute('open')).toBe(true);
    expect(elementoDialogo.getAttribute('aria-labelledby')).toBe(titulo?.id);
    expect(titulo?.textContent).toContain('Confirmar exclusão');
  });

  it('should close through the button and through the backdrop', async () => {
    const elemento = await criar();
    fixture.componentInstance.aberto.set(true);
    await fixture.whenStable();

    elemento.querySelector<HTMLButtonElement>('dialog button')?.click();
    await fixture.whenStable();
    expect(fixture.componentInstance.aberto()).toBe(false);
    expect(dialog(elemento).hasAttribute('open')).toBe(false);

    fixture.componentInstance.aberto.set(true);
    await fixture.whenStable();
    dialog(elemento).dispatchEvent(new MouseEvent('click', { bubbles: true }));
    await fixture.whenStable();
    expect(fixture.componentInstance.aberto()).toBe(false);
  });

  it('should not have critical axe violations when open', async () => {
    const elemento = await criar();
    fixture.componentInstance.aberto.set(true);
    await fixture.whenStable();

    expect(await violacoesCriticas(elemento)).toEqual([]);
  });
});
