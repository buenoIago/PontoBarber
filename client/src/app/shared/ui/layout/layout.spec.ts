import { Component, provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { violacoesCriticas } from '../../../../testing/axe';
import { Layout } from './layout';

@Component({
  imports: [Layout],
  template: `
    <app-layout>
      <span appLayoutHeader>Barbearia</span>
      <p>Conteúdo principal</p>
      <span appLayoutFooter>Rodapé</span>
    </app-layout>
  `,
})
class HostLayout {}

describe('Layout', () => {
  let fixture: ComponentFixture<HostLayout>;

  async function criar(): Promise<HTMLElement> {
    await TestBed.configureTestingModule({
      imports: [HostLayout],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();

    fixture = TestBed.createComponent(HostLayout);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('should expose landmarks and a skip link when rendered', async () => {
    const elemento = await criar();

    expect(elemento.querySelector('header')?.textContent).toContain('Barbearia');
    expect(elemento.querySelector('main')?.textContent).toContain('Conteúdo principal');
    expect(elemento.querySelector('footer')?.textContent).toContain('Rodapé');
    expect(elemento.querySelector('a[href="#conteudo"]')).not.toBeNull();
    expect(elemento.querySelector('main')?.id).toBe('conteudo');
  });

  it('should not have critical axe violations when rendered', async () => {
    expect(await violacoesCriticas(await criar())).toEqual([]);
  });
});
