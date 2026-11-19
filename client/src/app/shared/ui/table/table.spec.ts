import { Component, provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { violacoesCriticas } from '../../../../testing/axe';
import { Table } from './table';

@Component({
  imports: [Table],
  template: `
    <app-table legenda="Serviços disponíveis">
      <thead>
        <tr>
          <th scope="col">Nome</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td>Corte</td>
        </tr>
      </tbody>
    </app-table>
  `,
})
class HostTable {}

describe('Table', () => {
  let fixture: ComponentFixture<HostTable>;

  async function criar(): Promise<HTMLElement> {
    await TestBed.configureTestingModule({
      imports: [HostTable],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();

    fixture = TestBed.createComponent(HostTable);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('should render caption, responsive wrapper and semantic headers when rendered', async () => {
    const elemento = await criar();

    expect(elemento.querySelector('.table-responsive')).not.toBeNull();
    expect(elemento.querySelector('caption')?.textContent).toContain('Serviços disponíveis');
    expect(elemento.querySelector('th')?.getAttribute('scope')).toBe('col');
    expect(elemento.querySelector('tbody')?.textContent).toContain('Corte');
  });

  it('should not have critical axe violations when rendered', async () => {
    expect(await violacoesCriticas(await criar())).toEqual([]);
  });
});
