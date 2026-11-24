import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { violacoesCriticas } from '../../../testing/axe';
import { EscolhaPerfil } from './escolha-perfil';

@Component({
  imports: [EscolhaPerfil],
  template: `
    <app-escolha-perfil
      (selecionado)="perfil.set($event)"
      (entrar)="entrou.set('cliente')"
      (entrarProfissional)="entrou.set('profissional')"
    />
  `,
})
class Host {
  readonly perfil = signal<string | null>(null);
  readonly entrou = signal<string | null>(null);
}

describe('EscolhaPerfil', () => {
  let fixture: ComponentFixture<Host>;

  async function criar(): Promise<HTMLElement> {
    await TestBed.configureTestingModule({
      imports: [Host],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();

    fixture = TestBed.createComponent(Host);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('should emit the chosen profile and the login action', async () => {
    const elemento = await criar();
    const botoes = elemento.querySelectorAll<HTMLButtonElement>('app-button button');

    botoes[0].click();
    await fixture.whenStable();
    expect(fixture.componentInstance.perfil()).toBe('cliente');

    botoes[1].click();
    await fixture.whenStable();
    expect(fixture.componentInstance.perfil()).toBe('profissional');

    const entrar = elemento.querySelectorAll<HTMLButtonElement>('button.btn-link');
    entrar[0].click();
    await fixture.whenStable();
    expect(fixture.componentInstance.entrou()).toBe('cliente');

    entrar[1].click();
    await fixture.whenStable();
    expect(fixture.componentInstance.entrou()).toBe('profissional');
  });

  it('should not have critical axe violations when rendered', async () => {
    expect(await violacoesCriticas(await criar())).toEqual([]);
  });
});
