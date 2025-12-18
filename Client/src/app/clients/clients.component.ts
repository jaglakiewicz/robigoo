/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Component } from '@angular/core';

interface ClientRow {
  name: string;
  document: string;
  vehicle: string;
  statusKey: string;
}

@Component({
  selector: 'app-clients',
  templateUrl: './clients.component.html',
  styleUrls: ['./clients.component.css']
})
export class ClientsComponent {
  clients: ClientRow[] = [
    { name: 'Anna Kowalska', document: 'PL-09123', vehicle: 'VW Passat B8', statusKey: 'clients.status.active' },
    { name: 'Piotr Nowak', document: 'PL-44110', vehicle: 'Renault Master', statusKey: 'clients.status.active' },
    { name: 'Serhii Melnyk', document: 'UA-30011', vehicle: 'Skoda Octavia', statusKey: 'clients.status.blocked' },
    { name: 'Oksana Lytvyn', document: 'UA-11892', vehicle: 'Ford Transit', statusKey: 'clients.status.active' }
  ];
}

