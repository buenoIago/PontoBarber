import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { violacoesCriticas } from '../../../../testing/axe';
import { Button, Variante } from './button';

@Component({
  imports: [Button],
  template: `
    <app-button
      [variante]="variante()"
      [tamanho]="tamanho()"
      [tipo]="tipo()"
      [desabilitado]="desabilitado()"
      [rotuloAria]="rotuloAria()"
      (click)="cliques.set(cliques() + 1)"
    >
      Enviar
    </app-button>
  `,
})
class HostButton {
  readonly variante = signal<Variante>('primary');
  readonly tamanho = signal<'sm' | 'lg' | undefined>(undefined);
  readonly tipo = signal<'button' | 'submit' | 'reset'>('button');
  readonly desabilitado = signal(false);
  readonly rotuloAria = signal<string | undefined>(undefined);
  readonly cliques = signal(0);
}

describe('Button', () => {
  let fixture: ComponentFixture<HostButton>;

  async function criar(): Promise<HTMLElement> {
    await TestBed.configureTestingModule({
      imports: [HostButton],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();

    fixture = TestBed.createComponent(HostButton);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  function botao(elemento: HTMLElement): HTMLButtonElement {
    const encontrado = elemento.querySelector('button');
    if (!encontrado) throw new Error('botão não renderizado');
    return encontrado;
  }

  it('should apply bootstrap classes for variant and size when inputs change', async () => {
    const elemento = await criar();
    expect(botao(elemento).className).toContain('btn-primary');
    expect(botao(elemento).className).not.toContain('btn-lg');

    fixture.componentInstance.variante.set('secondary');
    fixture.componentInstance.tamanho.set('lg');
    await fixture.whenStable();

    expect(botao(elemento).className).toContain('btn-secondary');
    expect(botao(elemento).className).toContain('btn-lg');
  });

  it('should keep native type, label and disabled behaviour', async () => {
    const elemento = await criar();
    fixture.componentInstance.tipo.set('submit');
    fixture.componentInstance.rotuloAria.set('Confirmar agendamento');
    await fixture.whenStable();

    expect(botao(elemento).type).toBe('submit');
    expect(botao(elemento).getAttribute('aria-label')).toBe('Confirmar agendamento');
    expect(botao(elemento).textContent).toContain('Enviar');

    fixture.componentInstance.desabilitado.set(true);
    await fixture.whenStable();
    botao(elemento).click();
    expect(fixture.componentInstance.cliques()).toBe(0);
  });

  it('should not have critical axe violations when rendered', async () => {
    expect(await violacoesCriticas(await criar())).toEqual([]);
  });
});
