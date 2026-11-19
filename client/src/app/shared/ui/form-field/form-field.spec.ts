import { Component, provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { violacoesCriticas } from '../../../../testing/axe';
import { FormField } from './form-field';

@Component({
  imports: [FormField],
  template: `
    <app-form-field
      id="nome"
      rotulo="Nome"
      [obrigatorio]="true"
      dica="Como está no documento"
      erro="Nome inválido"
    >
      <input id="nome" type="text" />
    </app-form-field>
  `,
})
class HostFormField {}

describe('FormField', () => {
  let fixture: ComponentFixture<HostFormField>;

  async function criar(): Promise<HTMLElement> {
    await TestBed.configureTestingModule({
      imports: [HostFormField],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();

    fixture = TestBed.createComponent(HostFormField);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('should associate the label with the projected control when rendered', async () => {
    const elemento = await criar();

    expect(elemento.querySelector('label')?.getAttribute('for')).toBe('nome');
    expect(elemento.querySelector('#nome')).not.toBeNull();
    expect(elemento.querySelector('label')?.textContent).toContain('(obrigatório)');
  });

  it('should expose hint and error identifiers for aria-describedby', async () => {
    const elemento = await criar();

    expect(elemento.querySelector('#nome-dica')?.textContent).toContain('Como está no documento');
    expect(elemento.querySelector('#nome-erro')?.getAttribute('role')).toBe('alert');
    expect(elemento.querySelector('#nome-erro')?.textContent).toContain('Nome inválido');
  });

  it('should not have critical axe violations when rendered', async () => {
    expect(await violacoesCriticas(await criar())).toEqual([]);
  });
});
