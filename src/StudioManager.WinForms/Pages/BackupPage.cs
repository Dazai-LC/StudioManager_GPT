using StudioManager.Application.Services;
using StudioManager.WinForms.Controls;

namespace StudioManager.WinForms.Pages;

public sealed class BackupPage : UserControl
{
    private readonly AppFacade _app;
    public BackupPage(AppFacade app)
    {
        _app=app;BackColor=Theme.Background;
        var header=new PageHeader("Sao lưu & phục hồi","Bảo vệ dữ liệu studio bằng bản sao lưu SQL Server.");
        var card=new CardPanel{Dock=DockStyle.Top,Height=300,Padding=new Padding(28)};
        var icon=new Label{Text="◈",Dock=DockStyle.Left,Width=86,Font=Theme.Font(34,FontStyle.Bold),ForeColor=Theme.Primary,TextAlign=ContentAlignment.TopCenter};
        var content=new Panel{Dock=DockStyle.Fill,Padding=new Padding(16,0,0,0)};
        content.Controls.Add(new Label{Dock=DockStyle.Bottom,Height=88,Text="Lưu ý: Phục hồi sẽ thay thế toàn bộ dữ liệu hiện tại, đóng các kết nối và yêu cầu khởi động lại ứng dụng.",Font=Theme.Font(9.5f),ForeColor=Theme.Muted});
        var actions=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=62};
        var backup=Theme.Button("Tạo bản sao lưu");backup.Width=180;backup.Click+=async(_,_)=>await BackupAsync();
        var restore=Theme.Button("Phục hồi dữ liệu",Theme.Danger);restore.Width=180;restore.Click+=async(_,_)=>await RestoreAsync();
        actions.Controls.Add(backup);actions.Controls.Add(restore);content.Controls.Add(actions);
        content.Controls.Add(new Label{Dock=DockStyle.Top,Height=90,Text="Sao lưu cơ sở dữ liệu\nTệp .bak được tạo bởi SQL Server tại vị trí máy chủ có quyền ghi.",Font=Theme.Font(12),ForeColor=Theme.Text});
        card.Controls.Add(content);card.Controls.Add(icon);
        Controls.Add(card);Controls.Add(header);
    }
    private async Task BackupAsync(){using var s=new SaveFileDialog{Filter="SQL Server backup (*.bak)|*.bak",FileName=$"StudioManager_{DateTime.Now:yyyyMMdd_HHmmss}.bak"};if(s.ShowDialog(this)!=DialogResult.OK)return;var r=await _app.BackupRestore.BackupAsync(s.FileName,_app.Session!);if(!r.Success)Ui.Error(this,r.Message);else Ui.Info(this,r.Message);}
    private async Task RestoreAsync(){using var o=new OpenFileDialog{Filter="SQL Server backup (*.bak)|*.bak"};if(o.ShowDialog(this)!=DialogResult.OK)return;if(MessageBox.Show("Dữ liệu hiện tại sẽ bị thay thế. Bạn đã tạo bản sao lưu chưa?","Xác nhận bước 1/2",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;if(MessageBox.Show("Xác nhận lần cuối: tiếp tục phục hồi dữ liệu?","Xác nhận bước 2/2",MessageBoxButtons.YesNo,MessageBoxIcon.Stop)!=DialogResult.Yes)return;var r=await _app.BackupRestore.RestoreAsync(o.FileName,_app.Session!);if(!r.Success)Ui.Error(this,r.Message);else{Ui.Info(this,r.Message);System.Windows.Forms.Application.Restart();}}
}
