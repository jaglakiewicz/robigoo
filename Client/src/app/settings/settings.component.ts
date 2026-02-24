import { Component, OnInit, OnDestroy, ElementRef } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../services/auth.service';
import { UserService, CreateUserRequest, UserDTO } from '../services/user.service';
import { NotificationService } from '../services/notification.service';
import { SVG_ICONS } from '../shared/svg-icons';
import { Step } from '../shared/components/step-indicator/step-indicator.component';

@Component({
  selector: 'app-settings',
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.css']
})
export class SettingsComponent implements OnInit, OnDestroy {
  activeSection = 0;
  currentStep = 0; // kept for compatibility
  private editUserListener: any;
  SVG_ICONS = SVG_ICONS; // Make SVG_ICONS available in template

  steps: Step[] = [
    { id: 0, key: 'program', label: 'Ustawienia programu' },
    { id: 1, key: 'user', label: 'Ustawienia użytkownika' },
    { id: 2, key: 'admin', label: 'Zarządzanie użytkownikami' }
  ];

  // ── App Settings (full DTO mirroring backend AppSettingsData) ──────────
  appSettings = {
    printing: {
      defaultPrinter: '', paperSize: 'A4', orientation: 'Portrait',
      copies: 1, colorPrint: false, printHeader: true, printFooter: true,
      printPageNumbers: true, printWatermark: false, watermarkText: 'KOPIA',
      marginTopMm: 20, marginRightMm: 15, marginBottomMm: 20, marginLeftMm: 25,
      defaultXslTemplate: ''
    },
    protocols: {
      numberFormat: '{PREFIX}/{YEAR}/{SEQ}', numberPrefix: 'SKO',
      inspectionValidityYears: 3, sequenceResetPeriod: 'yearly',
      sequenceStartValue: 1, sequencePadding: 3,
      autoSaveOnCreate: true, requireClientOnCreate: false, requireSprayerOnCreate: true,
      expiryWarningDays: 30, defaultInspectionType: 'field',
      allowEditAfterSign: false, generatePdfOnCreate: false
    },
    organization: {
      stationName: '', accreditationNumber: '', addressLine1: '', addressLine2: '',
      postalCode: '', city: '', phone: '', email: '', website: '',
      taxId: '', bankAccount: '', logoBase64: '', logoMimeType: '',
      accreditationBody: '', accreditationScope: ''
    },
    display: {
      language: 'pl', theme: 'light', dateFormat: 'dd.MM.yyyy', timeFormat: 'HH:mm',
      decimalSeparator: ',', thousandsSeparator: ' ', timezone: 'Europe/Warsaw',
      currency: 'PLN', itemsPerPage: 25, showTooltips: true, compactMode: false
    },
    data: {
      autoSave: true, autoSaveIntervalSeconds: 30, backupPath: '',
      autoBackup: false, backupSchedule: 'daily', backupRetentionDays: 30,
      exportFormat: 'pdf', exportIncludeAttachments: true,
      maxAttachmentSizeMb: 10, archiveAfterYears: false, archiveAfterYearsValue: 5
    },
    security: {
      sessionTimeoutMinutes: 60, maxLoginAttempts: 5, lockoutDurationMinutes: 15,
      passwordMinLength: 6, passwordRequireUppercase: false,
      passwordRequireDigit: false, passwordRequireSpecial: false,
      forcePasswordChangeDays: 0, logSecurityEvents: true,
      securityLogRetentionDays: 90, allowMultipleSessions: true, requireTwoFactor: false
    },
    notifications: {
      emailEnabled: false, smtpHost: '', smtpPort: 587, smtpUseSsl: true,
      smtpUser: '', smtpPassword: '', emailFrom: '', emailFromName: '',
      notifyOnProtocolCreate: false, notifyOnProtocolExpiry: true,
      notifyDaysBeforeExpiry: 30, notifyRecipientsJson: '[]', inAppNotifications: true
    }
  };

  availablePrinters: string[] = [];
  savingAppSettings = false;

  readonly formatTokens = [
    { token: '{PREFIX}',  hint: 'Prefiks (np. SKO)' },
    { token: '{YEAR}',    hint: 'Rok (np. 2025)' },
    { token: '{MONTH}',   hint: 'Miesiąc (np. 06)' },
    { token: '{SEQ}',     hint: 'Numer sekwencyjny (np. 001)' },
    { token: '{SEQ4}',    hint: 'Numer 4-cyfrowy (np. 0001)' },
  ];

  // XSL Templates
  xslTemplates: { name: string; uploadedAt: string; size: number }[] = [];
  defaultXslTemplate = '';
  loadingXsl = false;
  uploadingXsl = false;
  xslError = '';
  xslSuccess = '';

  get protocolNumberPreview(): string {
    const now = new Date();
    const p = this.appSettings.protocols;
    return (p.numberFormat || '{PREFIX}/{YEAR}/{SEQ}')
      .replace('{PREFIX}', p.numberPrefix || 'SKO')
      .replace('{YEAR}',   String(now.getFullYear()))
      .replace('{MONTH}',  String(now.getMonth() + 1).padStart(2, '0'))
      .replace('{SEQ}',    '1'.padStart(p.sequencePadding || 3, '0'))
      .replace('{SEQ4}',   '1'.padStart(4, '0'));
  }

  insertToken(token: string): void {
    this.appSettings.protocols.numberFormat =
      (this.appSettings.protocols.numberFormat || '') + token;
  }

  // User Settings
  userSettings = {
    login: '',
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    permissionNumber: '',
    theme: 'light',
    avatar: null as string | null
  };

  // Password change
  passwordForm = {
    oldPassword: '',
    newPassword: '',
    confirmPassword: ''
  };
  changePasswordError = '';
  changePasswordSuccess = false;
  changingPassword = false;
  showPasswordForm = false;

  // Settings save messages
  userSettingsError = '';
  userSettingsSuccess = false;
  savingSettings = false;
  avatarError = '';
  avatarSuccess = false;
  signatureImage: string | null = null;
  signatureError = '';
  signatureSuccess = false;

  // Admin Settings
  adminSettings = {
    enableLogs: true,
    logLevel: 'info',
    maxUsers: 100
  };

  // User Management
  users: UserDTO[] = [];
  showCreateUserForm = false;
  newUserForm = {
    login: '',
    password: '',
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    permissionNumber: '',
    role: 'user'
  };
  creatingUser = false;

  // Password confirmation dialog
  showPasswordConfirmDialog = false;
  confirmDialogAction: 'delete' | 'create' = 'delete';
  userToConfirm: UserDTO | null = null;
  confirmDialogPassword = '';
  confirmDialogError = '';
  confirmingAction = false;
  deletingUserId: number | null = null;

  // Role change
  updatingRoleUserId: number | null = null;

  constructor(
    private authService: AuthService,
    private userService: UserService,
    private notificationService: NotificationService,
    private sanitizer: DomSanitizer,
    private el: ElementRef,
    private http: HttpClient
  ) {}

  ngOnInit(): void {
    try {
      console.log('[Settings] Component initialized');
      
      // Listener for edit user settings event
      this.editUserListener = (event: any) => {
        this.scrollToSection(1);
      };
      window.addEventListener('editUserEvent', this.editUserListener);

      this.loadUserSettings();
      this.loadUsers();
      this.loadPrinters();
      this.loadXslTemplates();
      this.loadAppSettings();
    } catch (error) {
      console.error('[Settings] Error during initialization:', error);
    }
  }

  ngOnDestroy(): void {
    window.removeEventListener('editUserEvent', this.editUserListener);
  }

  loadUserSettings(): void {
    const currentUser = this.authService.getCurrentUser();
    if (currentUser) {
      this.userSettings.login = currentUser.login;
      this.userSettings.firstName = currentUser.firstName;
      this.userSettings.lastName = currentUser.lastName;
      this.userSettings.email = currentUser.email;
      this.userSettings.phone = currentUser.phone;
      this.userSettings.permissionNumber = currentUser.permissionNumber;
      if (currentUser.avatarBase64) {
        this.userSettings.avatar = 'data:image/png;base64,' + currentUser.avatarBase64;
      }
      if ((currentUser as any).signatureBase64) {
        this.signatureImage = 'data:image/png;base64,' + (currentUser as any).signatureBase64;
      }
    }
  }

  loadPrinters(): void {
    this.http.get<{ printers: string[], defaultPrinter: string }>('/api/settings/printers').subscribe({
      next: (res) => {
        this.availablePrinters = res.printers;
        if (!this.appSettings.printing.defaultPrinter && res.defaultPrinter) {
          this.appSettings.printing.defaultPrinter = res.defaultPrinter;
        }
      },
      error: () => {}
    });
  }

  loadUsers(): void {
    this.userService.getUsers().subscribe(
      (data: any) => {
        if (data && Array.isArray(data)) {
          this.users = data;
        } else {
          console.error('[Settings] Unexpected response format:', data);
        }
      },
      (error: any) => {
        console.error('[Settings] Error loading users:', error);
      }
    );
  }

  saveUserSettings(): void {
    this.userSettingsError = '';
    this.userSettingsSuccess = false;
    this.savingSettings = true;

    const currentUser = this.authService.getCurrentUser();
    const isAdmin = currentUser && currentUser.role === 'admin';

    // Regular users can only edit: email, phone, theme
    // Admins can also edit: firstName, lastName, permissionNumber
    const updateData: any = {
      email: this.userSettings.email,
      phone: this.userSettings.phone,
      theme: this.userSettings.theme
    };

    // Only admins can modify firstName, lastName, and permissionNumber
    if (isAdmin) {
      updateData.firstName = this.userSettings.firstName;
      updateData.lastName = this.userSettings.lastName;
      updateData.permissionNumber = this.userSettings.permissionNumber;
    }

    this.userService.updateProfile(updateData).subscribe(
      (response) => {
        console.log('[Settings] Profile updated:', response);
        this.notificationService.success('Ustawienia zostały pomyślnie zapisane!');
        this.savingSettings = false;

        // Update current user in auth service
        const currentUser = this.authService.getCurrentUser();
        if (currentUser) {
          // Always update these fields
          currentUser.email = this.userSettings.email;
          currentUser.phone = this.userSettings.phone;
          
          // Update admin-only fields if user is admin
          if (isAdmin) {
            currentUser.firstName = this.userSettings.firstName;
            currentUser.lastName = this.userSettings.lastName;
            currentUser.permissionNumber = this.userSettings.permissionNumber;
          }

          sessionStorage.setItem('currentUser', JSON.stringify(currentUser));
        }
      },
      (error: any) => {
        console.error('[Settings] Error saving settings:', error);
        const errorMsg = error.error?.message || 'Błąd podczas zapisywania ustawień';
        this.notificationService.error(errorMsg);
        this.savingSettings = false;
      }
    );
  }

  togglePasswordForm(): void {
    this.showPasswordForm = !this.showPasswordForm;
    if (!this.showPasswordForm) {
      // Clear form when closing
      this.passwordForm = { oldPassword: '', newPassword: '', confirmPassword: '' };
      this.changePasswordError = '';
    }
  }

  changePassword(): void {
    if (this.passwordForm.newPassword !== this.passwordForm.confirmPassword) {
      this.notificationService.error('Hasła nie pasują do siebie');
      return;
    }

    if (this.passwordForm.newPassword.length < 6) {
      this.notificationService.error('Hasło musi mieć co najmniej 6 znaków');
      return;
    }

    this.changingPassword = true;

    this.userService.changePassword({
      oldPassword: this.passwordForm.oldPassword,
      newPassword: this.passwordForm.newPassword
    }).subscribe(
      (response) => {
        console.log('[Settings] Password changed:', response);
        this.notificationService.success('Hasło zostało zmienione pomyślnie!');
        this.changingPassword = false;
        this.passwordForm = { oldPassword: '', newPassword: '', confirmPassword: '' };
        this.showPasswordForm = false;
        this.passwordDrawerOpen = false;
      },
      (error: any) => {
        console.error('[Settings] Error changing password:', error);
        const errorMsg = error.error?.message || 'Błąd podczas zmiany hasła';
        this.notificationService.error(errorMsg);
        this.changingPassword = false;
      }
    );
  }

  onAvatarSelected(event: any): void {
    const file: File = event.target.files[0];

    if (file) {
      // Validate file size (max 2MB)
      if (file.size > 2097152) { // 2MB
        this.avatarError = 'Plik jest za duży (maksymalnie 2MB)';
        return;
      }

      // Validate file type
      if (!file.type.startsWith('image/')) {
        this.avatarError = 'Plik musi być obrazem (JPG, PNG, GIF)';
        return;
      }

      const reader = new FileReader();
      reader.onload = (e: any) => {
        this.userService.uploadAvatar(file).subscribe(
          (response) => {
            console.log('[Settings] Avatar uploaded:', response);
            // Reload current user to get updated avatar
            this.loadUserSettings();
            this.avatarSuccess = true;
            this.avatarError = '';
            setTimeout(() => { this.avatarSuccess = false; }, 3000);
          },
          (error: any) => {
            console.error('[Settings] Error uploading avatar:', error);
            this.avatarError = error.error?.message || 'Błąd podczas przesyłania avatara';
          }
        );
      };
      reader.readAsDataURL(file);
    }
  }

  onSignatureSelected(event: any): void {
    const file: File = event.target.files[0];
    if (!file) return;
    if (file.size > 2097152) { this.signatureError = 'Plik jest za duży (max 2MB)'; return; }
    if (!file.type.startsWith('image/')) { this.signatureError = 'Plik musi być obrazem'; return; }
    this.userService.uploadSignature(file).subscribe({
      next: (res) => {
        this.signatureImage = 'data:image/png;base64,' + res.signatureBase64;
        this.signatureSuccess = true;
        this.signatureError = '';
        setTimeout(() => { this.signatureSuccess = false; }, 3000);
      },
      error: (err) => { this.signatureError = err.error?.message || 'Błąd podczas przesłania podpisu'; }
    });
  }

  passwordDrawerOpen = false;
  createUserDrawerOpen = false;

  openPasswordDrawer(): void {
    this.passwordForm = { oldPassword: '', newPassword: '', confirmPassword: '' };
    this.passwordDrawerOpen = true;
  }
  closePasswordDrawer(): void { this.passwordDrawerOpen = false; }

  openCreateUserDrawer(): void {
    this.resetCreateUserForm();
    this.createUserDrawerOpen = true;
  }
  closeCreateUserDrawer(): void { this.createUserDrawerOpen = false; }

  createUserFromDrawer(): void {
    this.createUser();
    if (!this.creatingUser) this.createUserDrawerOpen = false;
  }

  canSubmitPassword(): boolean {
    return !!this.passwordForm.oldPassword &&
           !!this.passwordForm.newPassword &&
           this.passwordForm.newPassword === this.passwordForm.confirmPassword &&
           this.passwordForm.newPassword.length >= 6;
  }

  getPasswordStrengthClass(): string {
    const p = this.passwordForm.newPassword;
    if (!p) return '';
    let score = 0;
    if (p.length >= 8) score++;
    if (p.length >= 12) score++;
    if (/[A-Z]/.test(p)) score++;
    if (/[0-9]/.test(p)) score++;
    if (/[^A-Za-z0-9]/.test(p)) score++;
    if (score <= 1) return 'strength-weak';
    if (score <= 3) return 'strength-medium';
    return 'strength-strong';
  }

  getPasswordStrengthLabel(): string {
    const c = this.getPasswordStrengthClass();
    if (c === 'strength-weak') return 'Słabe';
    if (c === 'strength-medium') return 'Średnie';
    return 'Silne';
  }

  scrollToSection(sectionId: number): void {
    const el = this.el.nativeElement.querySelector(`#section-${sectionId}`);
    if (el) el.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  onActiveSection(id: string): void {
    const num = parseInt(id.replace('section-', ''), 10);
    if (!isNaN(num)) this.activeSection = num;
  }

  goToStep(stepId: number): void {
    this.scrollToSection(stepId);
  }

  get xslTemplateOptions() {
    return [
      { value: '', label: '— brak —' },
      ...this.xslTemplates.map(t => ({ value: t.name, label: t.name }))
    ];
  }

  get printerOptions() {
    return [
      { value: '', label: '— wybierz —' },
      ...this.availablePrinters.map(p => ({ value: p, label: p })),
      { value: '__pdf__', label: 'Zapisz jako PDF' }
    ];
  }

  readonly dateFormatOptions = [
    { value: 'dd.MM.yyyy', label: 'dd.MM.yyyy' },
    { value: 'yyyy-MM-dd', label: 'yyyy-MM-dd' },
    { value: 'MM/dd/yyyy', label: 'MM/dd/yyyy' },
  ];
  readonly paperSizeOptions = [
    { value: 'A4', label: 'A4' }, { value: 'A3', label: 'A3' }, { value: 'Letter', label: 'Letter' }
  ];
  readonly orientationOptions = [
    { value: 'Portrait', label: 'Pionowa (Portrait)' }, { value: 'Landscape', label: 'Pozioma (Landscape)' }
  ];
  readonly seqResetOptions = [
    { value: 'never', label: 'Nigdy' }, { value: 'yearly', label: 'Co rok' }, { value: 'monthly', label: 'Co miesiąc' }
  ];
  readonly inspectionTypeOptions = [
    { value: 'field', label: 'Polowy' }, { value: 'orchard', label: 'Sadowniczy' }
  ];
  readonly themeOptions = [
    { value: 'light', label: 'Jasny' }, { value: 'dark', label: 'Ciemny' }, { value: 'system', label: 'Systemowy' }
  ];
  readonly languageOptions = [
    { value: 'pl', label: 'Polski' }, { value: 'en', label: 'English' }
  ];
  readonly exportFormatOptions = [
    { value: 'pdf', label: 'PDF' }, { value: 'xlsx', label: 'Excel (XLSX)' }, { value: 'csv', label: 'CSV' }
  ];
  readonly backupScheduleOptions = [
    { value: 'daily', label: 'Codziennie' }, { value: 'weekly', label: 'Co tydzień' }, { value: 'monthly', label: 'Co miesiąc' }
  ];
  readonly itemsPerPageOptions = [
    { value: 10, label: '10' }, { value: 25, label: '25' }, { value: 50, label: '50' }, { value: 100, label: '100' }
  ];

  loadAppSettings(): void {
    this.http.get<any>('/api/settings').subscribe({
      next: (data) => { this.appSettings = { ...this.appSettings, ...data }; },
      error: () => {}
    });
  }

  saveAppSettings(): void {
    this.savingAppSettings = true;
    this.http.put('/api/settings', this.appSettings).subscribe({
      next: () => { this.notificationService.success('Ustawienia programu zapisane'); this.savingAppSettings = false; },
      error: (err) => { this.notificationService.error(err.error?.message || 'Błąd zapisu'); this.savingAppSettings = false; }
    });
  }

  get visibleSteps(): Step[] {
    const currentUser = this.authService.getCurrentUser();
    if (!currentUser || currentUser.role !== 'admin') {
      // Non-admin users see only steps 0 and 1
      return [this.steps[0], this.steps[1]];
    }
    // Admin users see all steps
    return this.steps;
  }

  isCurrentUserAdmin(): boolean {
    const currentUser = this.authService.getCurrentUser();
    return currentUser && currentUser.role === 'admin';
  }

  toggleCreateUserForm(): void {
    this.showCreateUserForm = !this.showCreateUserForm;
    if (!this.showCreateUserForm) {
      this.resetCreateUserForm();
    }
  }

  resetCreateUserForm(): void {
    const currentUser = this.authService.getCurrentUser();
    
    this.newUserForm = {
      login: '',
      password: '',
      firstName: '',
      lastName: '',
      email: '',
      phone: '',
      permissionNumber: '',
      role: (currentUser && currentUser.login !== 'admin') ? 'user' : 'user'
    };
  }

  canSelectRoleWhenCreating(): boolean {
    const currentUser = this.authService.getCurrentUser();
    // Only master admin can select role when creating users
    return currentUser && currentUser.login === 'admin';
  }

  createUser(): void {
    if (!this.newUserForm.login || !this.newUserForm.password || !this.newUserForm.firstName) {
      this.notificationService.error('Login, hasło i imię są wymagane');
      return;
    }

    this.creatingUser = true;

    const newUser: CreateUserRequest = {
      login: this.newUserForm.login,
      password: this.newUserForm.password,
      firstName: this.newUserForm.firstName,
      lastName: this.newUserForm.lastName,
      email: this.newUserForm.email,
      phone: this.newUserForm.phone,
      permissionNumber: this.newUserForm.permissionNumber,
      role: this.newUserForm.role
    };

    this.userService.createUser(newUser).subscribe(
      (response) => {
        console.log('[Settings] User created:', response);
        this.notificationService.success('Użytkownik został pomyślnie utworzony!');
        this.creatingUser = false;
        this.resetCreateUserForm();
        this.showCreateUserForm = false;
        this.createUserDrawerOpen = false;

        // Reload users list
        this.loadUsers();
      },
      (error: any) => {
        console.error('[Settings] Error creating user:', error);
        const errorMsg = error.error?.error || error.error?.message || 'Błąd podczas tworzenia użytkownika';
        this.notificationService.error(errorMsg);
        this.creatingUser = false;
      }
    );
  }

  editUser(user: UserDTO): void {
    window.dispatchEvent(new CustomEvent('editUserEvent', { detail: user }));
  }

  canDeleteUser(user: UserDTO): boolean {
    const currentUser = this.authService.getCurrentUser();
    if (!currentUser) return false;

    // Cannot delete self
    if (currentUser.userId === user.userId) return false;

    // Only admins can delete users
    if (currentUser.role !== 'admin') return false;

    // Master admin (login = admin) can delete anyone
    if (currentUser.login === 'admin') return true;

    // Regular admin can only delete regular users (not admins)
    return user.role !== 'admin';
  }

  openDeleteConfirmDialog(user: UserDTO): void {
    // Ask for password confirmation before deleting
    this.userToConfirm = user;
    this.confirmDialogAction = 'delete';
    this.showPasswordConfirmDialog = true;
  }

  confirmPasswordAction(): void {
    if (!this.confirmDialogPassword || !this.userToConfirm) {
      return;
    }

    this.confirmDialogError = '';
    this.confirmingAction = true;

    this.userService.deleteUser(this.userToConfirm.userId, this.confirmDialogPassword).subscribe(
      (response) => {
        console.log('[Settings] User deleted:', response);
        this.showPasswordConfirmDialog = false;
        this.confirmingAction = false;
        this.confirmDialogPassword = '';
        this.userToConfirm = null;

        // Reload users list
        this.loadUsers();
        this.notificationService.success('Użytkownik został pomyślnie usunięty!');
      },
      (error: any) => {
        console.error('[Settings] Error deleting user:', error);
        const errorMsg = error.error?.message || error.error?.error || 'Błąd podczas usuwania użytkownika';
        this.notificationService.error(errorMsg);
      }
    );
  }

  cancelPasswordConfirmDialog(): void {
    this.showPasswordConfirmDialog = false;
    this.confirmDialogPassword = '';
    this.confirmDialogError = '';
    this.userToConfirm = null;
  }

  canChangeUserRole(user: UserDTO): boolean {
    const currentUser = this.authService.getCurrentUser();
    if (!currentUser) return false;

    // Cannot change own role
    if (currentUser.userId === user.userId) return false;

    // Only master admin (login = admin) can change roles
    // Regular admin cannot change roles of existing users
    if (currentUser.login !== 'admin') return false;

    return true;
  }

  onRoleChanged(user: UserDTO): void {
    const currentUser = this.authService.getCurrentUser();
    
    // Validation - only master admin can change roles
    if (!currentUser || currentUser.login !== 'admin') {
      // Reload user to reset the role
      this.loadUsers();
      this.notificationService.error('Tylko master administrator może zmieniać role użytkowników');
      return;
    }

    // Validation - check if user can make this change
    if (!this.canChangeUserRole(user)) {
      // Reload user to reset the role
      this.loadUsers();
      this.notificationService.error('Brak uprawnień do zmiany tej roli');
      return;
    }

    this.updatingRoleUserId = user.userId;

    this.userService.updateUserRole(user.userId, user.role).subscribe(
      (response) => {
        console.log('[Settings] User role updated:', response);
        this.notificationService.success(`Rola użytkownika ${user.login} została zmieniona`);
        this.updatingRoleUserId = null;
      },
      (error: any) => {
        console.error('[Settings] Error updating user role:', error);
        const errorMsg = error.error?.message || error.error?.error || 'Błąd podczas zmiany roli użytkownika';
        this.notificationService.error(errorMsg);
        this.updatingRoleUserId = null;
        // Reload users to revert the role change
        this.loadUsers();
      }
    );
  }

  loadXslTemplates(): void {
    this.loadingXsl = true;
    this.http.get<{ templates: any[]; defaultTemplate: string }>('/api/settings/xsl-templates').subscribe({
      next: (res) => {
        this.xslTemplates = res.templates;
        this.defaultXslTemplate = res.defaultTemplate;
        this.loadingXsl = false;
      },
      error: () => { this.loadingXsl = false; }
    });
  }

  onXslFileSelected(event: any): void {
    const file: File = event.target.files[0];
    if (!file) return;
    if (!file.name.endsWith('.xsl')) { this.xslError = 'Dozwolone są tylko pliki .xsl'; return; }
    this.uploadingXsl = true;
    this.xslError = '';
    const fd = new FormData();
    fd.append('file', file);
    this.http.post<{ name: string }>('/api/settings/xsl-templates/upload', fd).subscribe({
      next: () => { this.uploadingXsl = false; this.xslSuccess = 'Przesłano!'; this.loadXslTemplates(); setTimeout(() => this.xslSuccess = '', 3000); },
      error: (err) => { this.uploadingXsl = false; this.xslError = err.error?.message || 'Błąd przesyłania'; }
    });
  }

  setDefaultXsl(name: string): void {
    this.http.put('/api/settings/xsl-templates/default', { name }).subscribe({
      next: () => { this.defaultXslTemplate = name; this.notificationService.success('Domyślny szablon ustawiony'); },
      error: (err) => { this.notificationService.error(err.error?.message || 'Błąd'); }
    });
  }

  deleteXsl(name: string): void {
    if (!confirm(`Usunąć szablon ${name}?`)) return;
    this.http.delete(`/api/settings/xsl-templates/${encodeURIComponent(name)}`).subscribe({
      next: () => { this.loadXslTemplates(); this.notificationService.success('Usunięto'); },
      error: () => { this.notificationService.error('Błąd usuwania'); }
    });
  }

  getSafeHtml(html: string): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(html);
  }
}
