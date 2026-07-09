import { Component, OnInit, OnDestroy } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { Subscription } from 'rxjs';
import { DashboardService, RecentInspection, RecentCropSprayer, UpcomingInspection, DashboardStatistics } from '../services/dashboard.service';
import { NavigationService } from '../services/navigation.service';
import { SVG_ICONS } from '../shared/svg-icons';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit, OnDestroy {
  recentInspections: RecentInspection[] = [];
  recentCropSprayers: RecentCropSprayer[] = [];
  upcomingInspections: UpcomingInspection[] = [];
  statistics: DashboardStatistics | null = null;

  upcomingLimit = 10;
  inspectionsLimit = 10;
  sprayersLimit = 10;
  limitOptions = [5, 10, 15, 20];

  // Upcoming inspections duration filter
  upcomingDuration = 'all';
  upcomingDurationOptions = [
    { value: 'all', label: 'Wszystkie' },
    { value: 'week', label: 'Następny tydzień' },
    { value: 'month', label: 'Następny miesiąc' },
    { value: 'quarter', label: 'Następny kwartał' },
    { value: 'year', label: 'Następny rok' },
    { value: '3years', label: 'Następne 3 lata' }
  ];
  private allUpcomingInspections: UpcomingInspection[] = [];

  statisticsYear = new Date().getFullYear();
  availableYears: number[] = [];

  loadingInspections = true;
  loadingSprayers = true;
  loadingUpcoming = true;
  loadingStats = true;

  // Lucide icons from shared SVG_ICONS
  iconInspections: SafeHtml;
  iconSprayers: SafeHtml;
  iconClock: SafeHtml;
  iconStats: SafeHtml;
  iconMaximize: SafeHtml;
  iconMinimize: SafeHtml;
  iconCircleCheck: SafeHtml;
  iconCircleX: SafeHtml;

  // Expand/collapse state: null = no widget expanded, or widget key
  expandedWidget: string | null = null;

  private subs: Subscription[] = [];

  constructor(
    private dashboardService: DashboardService,
    private navigationService: NavigationService,
    private sanitizer: DomSanitizer
  ) {
    this.iconInspections = this.getSafeHtml(SVG_ICONS.inspections);
    this.iconSprayers = this.getSafeHtml(SVG_ICONS.types);
    this.iconClock = this.getSafeHtml(SVG_ICONS.historyIcon);
    this.iconStats = this.getSafeHtml(SVG_ICONS.iconChartBar);
    this.iconMaximize = this.getSafeHtml(SVG_ICONS.maximize);
    this.iconMinimize = this.getSafeHtml(SVG_ICONS.minimize);
    this.iconCircleCheck = this.getSafeHtml(SVG_ICONS.iconCircleCheck);
    this.iconCircleX = this.getSafeHtml(SVG_ICONS.iconCircleX);
  }

  getSafeHtml(icon: string): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(icon);
  }

  ngOnInit(): void {
    const currentYear = new Date().getFullYear();
    this.availableYears = Array.from({ length: 6 }, (_, i) => currentYear - i);
    this.loadRecentInspections();
    this.loadRecentCropSprayers();
    this.loadUpcomingInspections();
    this.loadStatistics();
  }

  ngOnDestroy(): void {
    this.subs.forEach(s => s.unsubscribe());
  }

  loadRecentInspections(): void {
    this.loadingInspections = true;
    const sub = this.dashboardService.getRecentInspections(this.inspectionsLimit).subscribe({
      next: data => {
        this.recentInspections = data;
        this.loadingInspections = false;
      },
      error: () => { this.loadingInspections = false; }
    });
    this.subs.push(sub);
  }

  onInspectionsLimitChange(value: number): void {
    this.inspectionsLimit = value;
    this.loadRecentInspections();
  }

  loadRecentCropSprayers(): void {
    this.loadingSprayers = true;
    const sub = this.dashboardService.getRecentCropSprayers(this.sprayersLimit).subscribe({
      next: data => {
        this.recentCropSprayers = data;
        this.loadingSprayers = false;
      },
      error: () => { this.loadingSprayers = false; }
    });
    this.subs.push(sub);
  }

  onSprayersLimitChange(value: number): void {
    this.sprayersLimit = value;
    this.loadRecentCropSprayers();
  }

  loadUpcomingInspections(): void {
    this.loadingUpcoming = true;
    const sub = this.dashboardService.getUpcomingInspections(this.upcomingLimit).subscribe({
      next: data => {
        this.allUpcomingInspections = data;
        this.applyUpcomingFilter();
        this.loadingUpcoming = false;
      },
      error: () => { this.loadingUpcoming = false; }
    });
    this.subs.push(sub);
  }

  onLimitChange(value: number): void {
    this.upcomingLimit = value;
    this.loadUpcomingInspections();
  }

  onUpcomingDurationChange(value: string): void {
    this.upcomingDuration = value;
    this.applyUpcomingFilter();
  }

  private applyUpcomingFilter(): void {
    if (this.upcomingDuration === 'all') {
      this.upcomingInspections = [...this.allUpcomingInspections]
        .sort((a, b) => a.remainingDays - b.remainingDays);
      return;
    }

    const maxDays = this.getDurationMaxDays(this.upcomingDuration);
    this.upcomingInspections = this.allUpcomingInspections
      .filter(u => u.remainingDays <= maxDays)
      .sort((a, b) => a.remainingDays - b.remainingDays);
  }

  private getDurationMaxDays(duration: string): number {
    switch (duration) {
      case 'week': return 7;
      case 'month': return 30;
      case 'quarter': return 90;
      case 'year': return 365;
      case '3years': return 1095;
      default: return Infinity;
    }
  }

  // Navigation methods
  navigateToSprayer(serialNumber: string): void {
    this.navigationService.navigateTo('types', 'menu.types', { select: serialNumber });
  }

  navigateToInspection(id: number): void {
    this.navigationService.navigateTo('inspections', 'menu.inspections', { select: String(id) });
  }

  loadStatistics(): void {
    this.loadingStats = true;
    const sub = this.dashboardService.getStatistics(this.statisticsYear).subscribe({
      next: data => {
        this.statistics = data;
        this.loadingStats = false;
      },
      error: () => { this.loadingStats = false; }
    });
    this.subs.push(sub);
  }

  onStatisticsYearChange(year: number): void {
    this.statisticsYear = year;
    this.loadStatistics();
  }

  toggleExpand(widget: string): void {
    this.expandedWidget = this.expandedWidget === widget ? null : widget;
  }

  isExpanded(widget: string): boolean {
    return this.expandedWidget === widget;
  }

  getUrgencyClass(remainingDays: number): string {
    if (remainingDays <= 0) return 'urgency-overdue';
    if (remainingDays <= 30) return 'urgency-critical';
    if (remainingDays <= 90) return 'urgency-warning';
    return 'urgency-ok';
  }

  formatRemainingTime(days: number): string {
    if (days <= 0) {
      const absDays = Math.abs(days);
      if (absDays < 30) return `${absDays} dni po terminie`;
      const months = Math.floor(absDays / 30);
      return `${months} mies. po terminie`;
    }
    if (days === 1) return '1 dzień';
    if (days < 7) return `${days} dni`;
    if (days < 30) {
      const weeks = Math.floor(days / 7);
      return weeks === 1 ? '1 tydzień' : `${weeks} tyg.`;
    }
    const months = Math.floor(days / 30);
    if (months === 1) return '1 miesiąc';
    if (months < 5) return `${months} miesiące`;
    return `${months} mies.`;
  }

  formatDate(dateStr: string | undefined): string {
    if (!dateStr) return '—';
    const d = new Date(dateStr);
    return d.toLocaleDateString('pl-PL', { day: '2-digit', month: '2-digit', year: 'numeric' });
  }

  formatDateTime(dateStr: string | undefined): string {
    if (!dateStr) return '—';
    const d = new Date(dateStr);
    return d.toLocaleDateString('pl-PL', { day: '2-digit', month: '2-digit', year: 'numeric' })
      + ' ' + d.toLocaleTimeString('pl-PL', { hour: '2-digit', minute: '2-digit' });
  }

  getSprayerTypeName(type: string): string {
    switch (type) {
      case '00': return 'Polowy';
      case '01': return 'Sadowniczy';
      default: return type;
    }
  }

  getSprayerKindName(kind: string): string {
    switch (kind) {
      case '00': return 'Zawieszany';
      case '01': return 'Przyczepiany';
      case '02': return 'Samobieżny';
      case '03': return 'Inny';
      default: return kind;
    }
  }

  getMaxStatCount(): number {
    if (!this.statistics) return 1;
    const max = Math.max(...this.statistics.months.map(m => m.count));
    return max || 1;
  }

  getBarWidth(count: number): number {
    return (count / this.getMaxStatCount()) * 100;
  }

  isCurrentMonth(month: number): boolean {
    return month === new Date().getMonth() + 1;
  }

  getUrgencyRowClass(remainingDays: number): string {
    if (remainingDays <= 0) return 'row-urgency-overdue';
    if (remainingDays <= 30) return 'row-urgency-critical';
    if (remainingDays <= 90) return 'row-urgency-warning';
    return '';
  }
}
