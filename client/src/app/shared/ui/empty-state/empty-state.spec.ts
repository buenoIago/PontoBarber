import { Component, provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { violacoesCriticas } from '../../../../testing/axe';
import { EmptyState } from './empty-state';

@Component({
  imports: [EmptyState],
  template: `
    <app-empty-state titulo="Sem agendamentos" descricao="Volte mais tarde.">
      <button type="button">Atualizar</button>
    </app-empty-state>
  `,
})
class HostEmptyState {}

describe('EmptyState', () => {
  let fixture: ComponentFixture<HostEmptyState>;

  async function criar(): Promise<HTMLElement> {
    await TestBed.configureTestingModule({
      imports: [HostEmptyState],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();

    fixture = TestBed.createComponent(HostEmptyState);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('should render heading, description and projected action when rendered', async () => {
    const elemento = await criar();

    expect(elemento.querySelector('h2')?.textContent).toContain('Sem agendamentos');
    expect(elemento.querySelector('p')?.textContent).toContain('Volte mais tarde.');
    expect(elemento.querySelector('button')?.textContent).toContain('Atualizar');
    expect(elemento.querySelector('[role="status"]')).not.toBeNull();
  });

  it('should not have critical axe violations when rendered', async () => {
    expect(await violacoesCriticas(await criar())).toEqual([]);
  });
});
